using BookingService.Repositories;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BookingService.Messaging;

public class BookingEventConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;

    public BookingEventConsumer(IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.TryParse(_configuration["RabbitMQ:Port"], out var port) ? port : 5672,
            UserName = _configuration["RabbitMQ:Username"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest"
        };
        
        var connection = await factory.CreateConnectionAsync("booking-consumer");
        var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: "booking_queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );
        
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (sender, args) =>
        {
            try
            {
                var body = args.Body.ToArray();
                var json = System.Text.Encoding.UTF8.GetString(body);
                var message = System.Text.Json.JsonSerializer.Deserialize<PaymentStatusMessage>(json);

                if (message != null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IBookingWriteRepository>();
                    var newStatus = message.PaymentSucceeded
                        ? BookingService.Models.BookingStatus.Confirmed
                        : BookingService.Models.BookingStatus.Cancelled;
                    await repository.UpdateStatusAsync(message.BookingId, newStatus);
                }

                await channel.BasicAckAsync(args.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Consumer error: {ex.Message}");
                await channel.BasicNackAsync(args.DeliveryTag, false, false);
            }
        };

        await channel.BasicConsumeAsync("booking_queue", autoAck: false, consumer: consumer);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}