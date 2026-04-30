using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace BookingService.Messaging;

public class BookingEventPublisher : IBookingEventPublisher, IDisposable
{
    private IConnection? _connection;
    private const string NotificationQueue = "Notification";

    public async Task InitializeAsync(IConfiguration configuration)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.TryParse(configuration["RabbitMQ:Port"], out var port) ? port : 5672,
            UserName = configuration["RabbitMQ:Username"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };

        _connection = await factory.CreateConnectionAsync("booking-service");

        await using var channel = await _connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(
            queue: NotificationQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );
    }

    public async Task PublishNotificationMessage(NotificationMessage message)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        await using var channel = await _connection!.CreateChannelAsync();
        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: NotificationQueue,
            mandatory: false,
            basicProperties: props,
            body: body
        );
    }

    public Task PublishPaymentMessage(PaymentMessage message)
    {
        // To be implemented once Payment Service queue is ready
        throw new NotImplementedException();
    }

    public void Dispose() => _connection?.Dispose();
}