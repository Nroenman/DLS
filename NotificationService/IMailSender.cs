using MailKit.Security;
using MimeKit;

namespace Notification
{
    public interface IMailSender
    {

        Task SendEmailAsync(
            string fromEmail,
            string fromName,
            string toEmail,
            string subject,
            string body);
        
            
        
    }
}