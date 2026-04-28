using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;


namespace Notification
{
    public class MailSender: IMailSender
    {
        private string smtpHost = "smtp-relay.brevo.com";
        private int smtpPort = 587;
        private string smtpUsername = "a5de95001@smtp-brevo.com";
        private string smtpPassword = "QLxw2d8NMjbSOPYc";


        public async Task SendEmailAsync(
            string fromEmail,
            string fromName,
            string toEmail,
            string subject,
            string body)
        {
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(fromName, fromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            email.Body = new TextPart("html")
            {
                Text = body
            };

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(smtpUsername, smtpPassword);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
    
}
