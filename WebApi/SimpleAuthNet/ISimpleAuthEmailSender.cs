using System.Net.Mail;

namespace SimpleAuthNet;

public interface ISimpleAuthEmailSender
{
    Task SendAsync(MailMessage msg);
}
