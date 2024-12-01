using Microsoft.Extensions.Configuration;
using SimpleAuthNet.Models.Config;
using System.Net;
using System.Net.Mail;

namespace SimpleAuthNet;

public class EmailService(IConfiguration configuration)
{
    private readonly EmailSettings _emailSettings = configuration.GetSection("EmailSettings").Get<EmailSettings>()
                                                    ?? throw new InvalidOperationException("EmailSettings configuration is missing.");

    public void SendEmailMessage(MailMessage msg)
    {
        if (string.IsNullOrEmpty(_emailSettings.NoReplyAddress))
            throw new InvalidOperationException("NoReplyAddress is not configured.");

        msg.From ??= new MailAddress(_emailSettings.NoReplyAddress);

        if (_emailSettings.UseSmtpPickup)
        {
            if (string.IsNullOrEmpty(_emailSettings.SmtpPickupDirectory))
                throw new InvalidOperationException("SmtpPickupDirectory is not configured.");

            SaveToPickupDirectory(msg);
        }
        else
        {
            if (string.IsNullOrEmpty(_emailSettings.SmtpServer) || !_emailSettings.Port.HasValue)
                throw new InvalidOperationException("SmtpServer and Port must be configured.");

            using var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.Port.Value);
            smtpClient.EnableSsl = _emailSettings.EnableSsl;
            smtpClient.Credentials = !string.IsNullOrEmpty(_emailSettings.Username) && !string.IsNullOrEmpty(_emailSettings.Password)
                ? new NetworkCredential(_emailSettings.Username, _emailSettings.Password)
                : CredentialCache.DefaultNetworkCredentials;

            smtpClient.Send(msg);
        }
    }

    private void SaveToPickupDirectory(MailMessage msg)
    {
        if (!Directory.Exists(_emailSettings.SmtpPickupDirectory))
            Directory.CreateDirectory(_emailSettings.SmtpPickupDirectory);

        var smtpWriter = new SmtpClient
        {
            DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
            PickupDirectoryLocation = _emailSettings.SmtpPickupDirectory
        };

        smtpWriter.Send(msg);
    }
}
