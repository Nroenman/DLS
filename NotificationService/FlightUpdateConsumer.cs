using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notification;

public class FlightUpdateConsumer : BackgroundService
{
    private readonly MailSender _mailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FlightUpdateConsumer> _logger;
    private const string ExchangeName = "airport.flights";
    private const string QueueName    = "notification.flight.updated";
    private const string RoutingKey   = "flight.updated";

    public FlightUpdateConsumer(
        MailSender mailSender,
        IConfiguration configuration,
        ILogger<FlightUpdateConsumer> logger)
    {
        _mailSender    = mailSender;
        _configuration = configuration;
        _logger        = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"]     ?? "localhost",
            Port     = int.TryParse(_configuration["RabbitMQ:Port"], out var p) ? p : 5672,
            UserName = _configuration["RabbitMQ:Username"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest",
        };

        var connection = factory.CreateConnection("notification-service");
        var channel    = connection.CreateModel();

        channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
        channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(QueueName, ExchangeName, RoutingKey);
        channel.BasicQos(0, 1, false);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json  = Encoding.UTF8.GetString(ea.Body.ToArray());
                var event_ = JsonSerializer.Deserialize<FlightUpdatedEvent>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (event_ is null)
                {
                    channel.BasicNack(ea.DeliveryTag, false, false);
                    return;
                }

                _logger.LogInformation(
                    "Flight {FlightNumber} updated — status: {Status}",
                    event_.FlightNumber, event_.Status);

                if (event_.FollowerEmails.Count > 0)
                    await NotifyFollowersAsync(event_);

                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing flight update message");
                channel.BasicNack(ea.DeliveryTag, false, requeue: false);
            }
        };

        channel.BasicConsume(QueueName, autoAck: false, consumer);

        stoppingToken.Register(() =>
        {
            channel.Close();
            connection.Close();
        });

        return Task.CompletedTask;
    }

    private async Task NotifyFollowersAsync(FlightUpdatedEvent ev)
    {
        var fromEmail = _configuration["Mail:FromEmail"] ?? "noreply@airport.dk";
        var fromName  = _configuration["Mail:FromName"]  ?? "Airport Notification Service";

        var subject = $"Flight {ev.FlightNumber} update: {ev.Status}";
        var body    = BuildEmailBody(ev);

        foreach (var recipientEmail in ev.FollowerEmails)
        {
            try
            {
                await _mailSender.SendEmailAsync(fromEmail, fromName, recipientEmail, subject, body);
                _logger.LogInformation("Notification sent to {Email}", recipientEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to {Email}", recipientEmail);
            }
        }
    }

    private static string BuildEmailBody(FlightUpdatedEvent ev)
    {
        var delayInfo = string.IsNullOrWhiteSpace(ev.DelayReason)
            ? string.Empty
            : $"<p><strong>Reason:</strong> {ev.DelayReason}</p>";

        return $"""
            <h2>Flight Update: {ev.FlightNumber}</h2>
            <p><strong>Airline:</strong> {ev.Airline}</p>
            <p><strong>Route:</strong> {ev.Origin} → {ev.Destination}</p>
            <p><strong>Status:</strong> {ev.Status}</p>
            {delayInfo}
            <p><strong>Scheduled departure:</strong> {ev.ScheduledDeparture:yyyy-MM-dd HH:mm} UTC</p>
            <p><strong>Scheduled arrival:</strong> {ev.ScheduledArrival:yyyy-MM-dd HH:mm} UTC</p>
            """;
    }
}

public record FlightUpdatedEvent(
    Guid Id,
    string FlightNumber,
    string Airline,
    string Origin,
    string Destination,
    string Status,
    string? DelayReason,
    DateTime ScheduledDeparture,
    DateTime ScheduledArrival,
    DateTime? ActualDeparture,
    DateTime? ActualArrival,
    DateTime UpdatedAt,
    List<string> FollowerEmails
);
