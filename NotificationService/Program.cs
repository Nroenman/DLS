using Notification;


class Program
{
    static Task Main(string[] args)
    {
        Console.WriteLine("Starter notification service...");

        MailSender mailSender = new MailSender();

        RabbitMq rabbitMQ = new(new MailSender());
        while (true)
        {
          rabbitMQ.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

        }
    }
}
