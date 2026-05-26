using System.Net;
using System.Text;
using System.Text.Json;
using AirportSystem.Flights.Models;
using RabbitMQ.Client;

namespace AirportSystem.Flights.Services.Messaging;

/// <summary>
/// Publishes one ready-to-send <see cref="NotificationMessage"/> per follower
/// onto the NotificationService's queue whenever a flight is updated. The
/// NotificationService stays a generic email worker; the flight-domain-to-email
/// translation happens here.
/// </summary>
public class RabbitMqFlightEventPublisher : IFlightEventPublisher, IDisposable
{
    private const string FromName = "Airport Flight Updates";

    private readonly IConnection _connection;
    private readonly string _queueName;

    public RabbitMqFlightEventPublisher(IConfiguration configuration)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            Port     = int.TryParse(configuration["RabbitMQ:Port"], out var port) ? port : 5672,
            UserName = configuration["RabbitMQ:Username"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest",
        };
        _connection = factory.CreateConnection("flight-service");
        _queueName  = configuration["RabbitMQ:NotificationQueue"] ?? "Notification";

        // Declare the queue with the same settings the NotificationService uses,
        // so delivery works regardless of which service starts first.
        using var channel = _connection.CreateModel();
        channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
    }

    public void PublishFlightUpdated(Flight flight)
    {
        var followerEmails = flight.Followers
            .Where(f => !string.IsNullOrWhiteSpace(f.User?.Email))
            .Select(f => f.User!.Email)
            .Distinct()
            .ToList();

        if (followerEmails.Count == 0)
            return;

        var subject = $"Flight {flight.FlightNumber} update — {flight.Status}";
        var body    = BuildBody(flight);

        using var channel = _connection.CreateModel();
        var props = channel.CreateBasicProperties();
        props.Persistent  = true;
        props.ContentType = "application/json";

        foreach (var email in followerEmails)
        {
            var message = new NotificationMessage(FromName, email, subject, body);
            var payload = JsonSerializer.SerializeToUtf8Bytes(message);

            // Publish straight to the queue via the default ("") exchange.
            channel.BasicPublish(
                exchange: string.Empty,
                routingKey: _queueName,
                basicProperties: props,
                body: payload);
        }
    }

    private static string BuildBody(Flight flight)
    {
        static string Enc(string value) => WebUtility.HtmlEncode(value);

        var sb = new StringBuilder();
        sb.Append("<p>Hello,</p>");
        sb.Append($"<p>There is an update for flight <strong>{Enc(flight.FlightNumber)}</strong> ")
          .Append($"({Enc(flight.Airline)}), {Enc(flight.Origin)} &rarr; {Enc(flight.Destination)}.</p>");
        sb.Append("<ul>");
        sb.Append($"<li>Status: {flight.Status}</li>");
        if (!string.IsNullOrWhiteSpace(flight.DelayReason))
            sb.Append($"<li>Reason: {Enc(flight.DelayReason)}</li>");
        sb.Append(flight.Gate is not null
            ? $"<li>Gate: {Enc(flight.Gate.GateNumber)} (Terminal {Enc(flight.Gate.Terminal)})</li>"
            : "<li>Gate: not assigned</li>");
        sb.Append($"<li>Scheduled departure: {flight.ScheduledDeparture:yyyy-MM-dd HH:mm} UTC</li>");
        sb.Append($"<li>Scheduled arrival: {flight.ScheduledArrival:yyyy-MM-dd HH:mm} UTC</li>");
        if (flight.ActualDeparture.HasValue)
            sb.Append($"<li>Actual departure: {flight.ActualDeparture:yyyy-MM-dd HH:mm} UTC</li>");
        if (flight.ActualArrival.HasValue)
            sb.Append($"<li>Actual arrival: {flight.ActualArrival:yyyy-MM-dd HH:mm} UTC</li>");
        sb.Append("</ul>");
        sb.Append("<p>You are receiving this email because you follow this flight.</p>");
        return sb.ToString();
    }

    public void Dispose() => _connection.Dispose();
}
