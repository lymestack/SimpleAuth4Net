namespace SimpleAuthNet.Models.Config;

public class AppConfig
{
    public Environment Environment { get; set; }

    public CaptchaSettings CaptchaSettings { get; set; }

    public string GoogleClientId { get; set; }

    public Guid SessionId { get; set; }
}