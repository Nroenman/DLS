using RabbitMQ.Client;
using BookingService.Messaging;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using Moq;
using BookingService.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace BookingService.Tests.Integration;

public class RabbitMqIntegrationTests : IAsyncLifetime
{
    private BookingEventPublisher _publisher;
    private IConnection _consumerConnection;
    private IChannel _consumerChannel;

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "RabbitMQ:Host", "localhost" },
                { "RabbitMQ:Port", "5672" },
                { "RabbitMQ:Username", "guest" },
                { "RabbitMQ:Password", "guest" },
                { "DisableMessaging", "false" }
            })
            .Build();

        _publisher = new BookingEventPublisher();
        await _publisher.InitializeAsync(configuration);

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        };

        _consumerConnection = await factory.CreateConnectionAsync("rabbitmq-integration-test");
        _consumerChannel = await _consumerConnection.CreateChannelAsync();
    }
    
    [Fact]
    public async Task PublishNotificationMessage_MessageArrivesOnQueue_WithCorrectContent()
    {
        await _consumerChannel.QueuePurgeAsync("Notification");
    
        var message = new NotificationMessage
        {
            FromName = "Airport System",
            ToEmail = "test@test.com",
            Subject = "Booking Confirmed",
            Body = "Your booking has been confirmed."
        };

        await _publisher.PublishNotificationMessage(message);

        await Task.Delay(500);

        var result = await _consumerChannel.BasicGetAsync("Notification", autoAck: true);

        Assert.NotNull(result);

        var json = Encoding.UTF8.GetString(result.Body.ToArray());
        var received = JsonSerializer.Deserialize<NotificationMessage>(json);

        Assert.Equal(message.FromName, received.FromName);
        Assert.Equal(message.ToEmail, received!.ToEmail);
        Assert.Equal(message.Subject, received.Subject);
        Assert.Equal(message.Body, received.Body);
    }
    
    [Fact]
    public async Task PublishPaymentMessage_MessageArrivesOnQueue_WithCorrectContent()
    {
        await _consumerChannel.QueuePurgeAsync("payment_queue");

        var message = new PaymentMessage
        {
            BookingId = Guid.NewGuid(),
            UserId = "test-user-123",
            TotalPrice = 1500,
            ContactEmail = "test@test.com",
            ContactPhone = "12345678"
        };

        await _publisher.PublishPaymentMessage(message);

        await Task.Delay(500);

        var result = await _consumerChannel.BasicGetAsync("payment_queue", autoAck: true);

        Assert.NotNull(result);

        var json = Encoding.UTF8.GetString(result.Body.ToArray());
        var received = JsonSerializer.Deserialize<PaymentMessage>(json);

        Assert.Equal(message.BookingId, received!.BookingId);
        Assert.Equal(message.UserId, received.UserId);
        Assert.Equal(message.TotalPrice, received.TotalPrice);
        Assert.Equal(message.ContactEmail, received.ContactEmail);
        Assert.Equal(message.ContactPhone, received.ContactPhone);
    }

    public async Task DisposeAsync()
    {
        await _consumerChannel.CloseAsync();
        await _consumerConnection.CloseAsync();
        _publisher.Dispose();
    }
}