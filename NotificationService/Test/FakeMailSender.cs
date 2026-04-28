using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notification.Test
{
    public class FakeMailSender : IMailSender
    {
        public List<NotificationMessage> SentMessages { get; } = new();

        public Task SendEmailAsync(
            string fromEmail,
            string fromName,
            string toEmail,
            string subject,
            string body)
        {
            SentMessages.Add(new NotificationMessage
            {
                FromName = fromName,
                ToEmail = toEmail,
                Subject = subject,
                Body = body
            });

            return Task.CompletedTask;
        }
        public Task SendEmailAsyncfail(
        string fromEmail,
        string fromName,
        string toEmail,
        string subject,
        string body)
        {
            throw new Exception("Mail fejlede");
        }
    }
}
