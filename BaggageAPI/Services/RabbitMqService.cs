using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace BaggageAPI.Services;

public class RabbitMqService : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqService(IConfiguration config)
    {
      var factory = new ConnectionFactory
{
    HostName = config["RabbitMQ__Host"] ?? "localhost",
    Port = int.Parse(config["RabbitMQ__Port"] ?? "5672"),
    UserName = config["RabbitMQ__Username"] ?? "guest",
    Password = config["RabbitMQ__Password"] ?? "guest"
};

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public void Publish(string queueName, object message)
    {
        _channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true; // 👈 messages survive a RabbitMQ restart

        _channel.BasicPublish(
            exchange: "",
            routingKey: queueName,
            basicProperties: properties,
            body: body);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}