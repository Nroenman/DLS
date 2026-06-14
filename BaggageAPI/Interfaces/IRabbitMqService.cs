namespace BaggageAPI.Interfaces;

public interface IRabbitMqService
{
    void Publish(string queueName, object message);
}