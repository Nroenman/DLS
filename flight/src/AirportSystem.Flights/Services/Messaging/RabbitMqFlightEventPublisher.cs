using System.Text;
using System.Text.Json;
using AirportSystem.Flights.Models;
using RabbitMQ.Client;

namespace AirportSystem.Flights.Services.Messaging;

public class RabbitMqFlightEventPublisher : IFlightEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private const string ExchangeName = "airport.flights";

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

        using var channel = _connection.CreateModel();
        channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
    }

    public void PublishFlightUpdated(Flight flight)
    {
        var payload = new
        {
            flight.Id,
            flight.FlightNumber,
            flight.Airline,
            flight.Origin,
            flight.Destination,
            Status        = flight.Status.ToString(),
            flight.DelayReason,
            flight.ScheduledDeparture,
            flight.ScheduledArrival,
            flight.ActualDeparture,
            flight.ActualArrival,
            flight.UpdatedAt,
            FollowerEmails = flight.Followers
                .Where(f => f.User?.Email is not null)
                .Select(f => f.User!.Email)
                .ToList()
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

        using var channel = _connection.CreateModel();
        var props = channel.CreateBasicProperties();
        props.Persistent   = true;
        props.ContentType  = "application/json";

        channel.BasicPublish(ExchangeName, "flight.updated", props, body);
    }

    public void Dispose() => _connection.Dispose();
}
