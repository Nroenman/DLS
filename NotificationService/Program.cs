
using Notification;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starter notification service...");

       

        string fromEmail = "jeso0005@stud.ek.dk";
        string fromName = "School Notification Service";

        string toEmail = "dehli11111@gmail.com";
        string subject = "Test notifikation";
        string body = "<h2>Hej</h2><p>Dette er en testmail fra min C# console notification app.</p>";

        MailSender mailSender = new MailSender();

        try
        {
            await mailSender.SendEmailAsync(
                fromEmail,
                fromName,
                toEmail,
                subject,
                body
            );

            Console.WriteLine("Mail sendt!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Fejl ved afsendelse:");
            Console.WriteLine(ex.Message);
        }

        Console.WriteLine("Tryk på en tast for at afslutte...");
        Console.ReadKey();
    }
}