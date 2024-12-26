using Newtonsoft.Json;
using SimpleAuthNet.Models.Config;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace SimpleAuthNet;

public interface ISmsProvider
{
    public SmsSettings SmsSettings { get; set; }

    Task SendSmsAsync(string phoneNumber, string message);
}

public class TwilioSmsProvider : ISmsProvider
{
    public TwilioSmsProvider(SmsSettings smsSettings)
    {
        SmsSettings = smsSettings;

        TwilioClient.Init(smsSettings.AccountSid, smsSettings.AuthToken);
    }

    public SmsSettings SmsSettings { get; set; }

    public async Task SendSmsAsync(string phoneNumber, string message)
    {
        phoneNumber = $"+1{phoneNumber}";
        var messageOptions = new CreateMessageOptions(new PhoneNumber(phoneNumber))
        {
            From = new PhoneNumber(SmsSettings.SenderNumber),
            Body = message
        };

        await MessageResource.CreateAsync(messageOptions);
    }
}

public class SmsService
{
    private readonly ISmsProvider _smsProvider;
    private readonly string _logDirectory;

    public SmsService(ISmsProvider smsProvider, string logDirectory)
    {
        _smsProvider = smsProvider ?? throw new ArgumentNullException(nameof(smsProvider));
        _logDirectory = logDirectory;

        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
    }

    public async Task SendSmsAsync(string phoneNumber, string message)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number cannot be null or empty.", nameof(phoneNumber));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be null or empty.", nameof(message));

        if (!_smsProvider.SmsSettings.SimulateSend) await _smsProvider.SendSmsAsync(phoneNumber, message);
        LogSms(phoneNumber, message);
    }

    private void LogSms(string phoneNumber, string message)
    {
        var logEntry = new
        {
            PhoneNumber = phoneNumber,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        var logFileName = Path.Combine(_logDirectory, $"{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.json");
        var logContent = JsonConvert.SerializeObject(logEntry, Formatting.Indented);

        File.WriteAllText(logFileName, logContent);
    }
}
