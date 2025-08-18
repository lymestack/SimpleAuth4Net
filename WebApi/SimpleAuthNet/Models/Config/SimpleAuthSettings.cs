namespace SimpleAuthNet.Models.Config;

public class SimpleAuthSettings
{
    public Environment Environment { get; set; }

    public string GoogleClientId { get; set; }

    public Guid SessionId { get; set; }

    public bool EnableLocalAccounts { get; set; }

    public bool AllowRegistration { get; set; }

    public bool RequireUserVerification { get; set; }

    public bool EnableMfaViaEmail { get; set; }

    public bool EnableMfaViaSms { get; set; }

    public bool EnableMfaViaOtp { get; set; }

    public int ResendCodeDelaySeconds { get; set; }

    public PasswordComplexityOptions PasswordComplexity { get; set; }

    public List<ClientSsoProviderSettings> SsoProviders { get; set; } = new();
}