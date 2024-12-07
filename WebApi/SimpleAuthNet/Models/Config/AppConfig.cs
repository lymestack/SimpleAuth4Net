namespace SimpleAuthNet.Models.Config;

public class AppConfig
{
    public Environment Environment { get; set; }

    public string GoogleClientId { get; set; }

    public Guid SessionId { get; set; }

    public bool EnableLocalAccounts { get; set; }

    public bool EnableGoogle { get; set; }

    public bool AllowRegistration { get; set; }

    public bool RequireUserVerification { get; set; }
}