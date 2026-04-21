using Microsoft.Extensions.Configuration;
using System.Net.Mail;

namespace SimpleAuthNet;

public class DefaultSimpleAuthEmailSender(IConfiguration configuration) : ISimpleAuthEmailSender
{
    public Task SendAsync(MailMessage msg)
    {
        var inner = new EmailService(configuration);
        inner.SendEmailMessage(msg);
        return Task.CompletedTask;
    }
}
