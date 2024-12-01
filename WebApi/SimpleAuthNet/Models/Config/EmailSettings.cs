namespace SimpleAuthNet.Models.Config;

public class EmailSettings
{
    public string NoReplyAddress { get; set; } = string.Empty;
    public bool UseSmtpPickup { get; set; }
    public string SmtpPickupDirectory { get; set; } = string.Empty;
    public bool EnableSsl { get; set; }
    public string SmtpServer { get; set; } = string.Empty;
    public int? Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
