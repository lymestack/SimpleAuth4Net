namespace SimpleAuthNet.Models.Config;

public class SmsSettings
{
    public string Provider { get; set; }
    public string AccountSid { get; set; }
    public string AuthToken { get; set; }
    public string SenderNumber { get; set; }
    public string LogDirectory { get; set; }
    public bool SimulateSend { get; set; }
}