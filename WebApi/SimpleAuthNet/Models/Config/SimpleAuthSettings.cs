namespace SimpleAuthNet.Models.Config;

public class SimpleAuthSettings
{
    public Environment Environment { get; set; }

    public string GoogleClientId { get; set; }

    public Guid SessionId { get; set; }

    public bool EnableLocalAccounts { get; set; }

    public bool EnableGoogleSso { get; set; }

    public bool EnableFacebookSso { get; set; }

    public string FacebookAppId { get; set; }

    public bool AllowRegistration { get; set; }

    public bool RequireUserVerification { get; set; }

    public bool EnableMfaViaEmail { get; set; }

    public bool EnableMfaViaSms { get; set; }

    public bool EnableMfaViaOtp { get; set; }

    public int ResendCodeDelaySeconds { get; set; }

    public bool EnableMicrosoftSso { get; set; }

    public string MicrosoftTenantId { get; set; }

    public string MicrosoftClientId { get; set; }

    public string MicrosoftRedirectUri { get; set; }

    public bool EnableAppleSso { get; set; }
}