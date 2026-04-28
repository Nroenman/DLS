using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Notification
{
    public class RabbitMq
    {
        private const string FromEmail = "jeso0005@stud.ek.dk";
        private readonly IMailSender _mailSender;
        private readonly ConnectionFactory _factory;
        private readonly string _queueName;
        public RabbitMq(IMailSender mailSender,
            string hostName = "rabbitmq",
            string queueName = "Notification")
        {
            _mailSender = mailSender;
            _queueName = queueName;

            _factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = "guest",
                Password = "guest",
                DispatchConsumersAsync = true
            };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var connection = _factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.Received += async (_, ea) =>
            {
                await HandleMessageAsync(channel, ea);
            };

            channel.BasicConsume(
                queue: _queueName,
                autoAck: false,
                consumer: consumer
            );

            Console.WriteLine("Lytter efter beskeder...");

            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("RabbitMQ listener stoppet.");
            }
        }

        public async Task HandleMessageAsync(IModel channel, BasicDeliverEventArgs ea)
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                Console.WriteLine($"Modtaget besked: {json}");

                var message = JsonSerializer.Deserialize<NotificationMessage>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (message == null)
                    throw new Exception("Kunne ikke læse beskeden.");

                await _mailSender.SendEmailAsync(
                    FromEmail,
                    message.FromName,
                    message.ToEmail,
                    message.Subject,
                    message.Body
                );

                Console.WriteLine("Mail sendt!");

                channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fejl ved behandling af besked: {ex.Message}");

                channel.BasicNack(
                    ea.DeliveryTag,
                    multiple: false,
                    requeue: false
                );
            }
        }

    }
}