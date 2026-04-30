using Notification;


class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starter notification service...");

        string fromEmail = "";
        string fromName = "School Notification Service";

        string toEmail = "dehli11111@gmail.com";
        string subject = "Test notifikation";
        string body = "<h2>Hej</h2><p>Dette er en testmail fra min C# console notification app.</p>";

        MailSender mailSender = new MailSender();

        RabbitMq rabbitMQ = new();
        while (true)
        {
            rabbitMQ.StartAsync().GetAwaiter().GetResult();
        }
    }
}
