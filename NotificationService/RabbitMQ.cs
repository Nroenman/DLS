using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Notification
{
    public class RabbitMq
    {


        public RabbitMq()
        {

        }

        public async Task StartAsync()
        {
            var factory = new ConnectionFactory
            {
                HostName = "rabbitmq",
                UserName = "guest",
                Password = "guest",
                DispatchConsumersAsync = true
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            var queueName = "Notification";

            channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.Received += async (sender, ea) =>
            {
                try
                {
                    var bodyBytes = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(bodyBytes);

                    Console.WriteLine($"Modtaget besked: {json}");

                    var message = JsonSerializer.Deserialize<NotificationMessage>(
                         json,
                         new JsonSerializerOptions
                         {
                             PropertyNameCaseInsensitive = true
                         }
                     );

                    if (message == null)
                    {
                        throw new Exception("Kunne ikke læse beskeden.");
                    }

                    MailSender mailSender = new MailSender();
                    try
                    {
                        await mailSender.SendEmailAsync(
                        "jeso0005@stud.ek.dk",
                        message.FromName,
                        message.ToEmail,
                        message.Subject,
                        message.Body
                        );

                        Console.WriteLine("Mail sendt!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Fejl ved afsendelse:");
                        Console.WriteLine(ex.Message);
                    }

                    channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fejl ved behandling af besked: {ex.Message}");

                    // false = læg ikke beskeden tilbage i køen
                    channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            channel.BasicConsume(
                queue: queueName,
                autoAck: false,
                consumer: consumer
            );

            Console.WriteLine("Lytter efter beskeder...");

            await Task.Delay(Timeout.Infinite);
        }
    }

}