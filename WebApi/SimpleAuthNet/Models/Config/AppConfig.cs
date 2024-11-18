namespace SimpleAuthNet.Models.Config;

public class AppConfig
{
    public Environment Environment { get; set; }

    public AuthSettings AuthSettings { get; set; }

    public CaptchaSettings CaptchaSettings { get; set; }

    public Guid SessionId { get; set; }
}