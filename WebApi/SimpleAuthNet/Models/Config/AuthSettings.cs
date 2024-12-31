namespace SimpleAuthNet.Models.Config;

public class AuthSettings
{
    public string TokenSecret { get; set; } = "";

    public int AccessTokenExpirationMinutes { get; set; }

    public bool UseRefreshTokens { get; set; }

    public int RefreshTokenExpirationDays { get; set; }

    public bool StoreTokensInCookies { get; set; }

    public int VerifyTokenExpiresInMinutes { get; set; }

    public int MaxFailedLoginAttempts { get; set; }

    public int AccountLockoutDurationInMinutes { get; set; }

    public PasswordComplexityOptions PasswordComplexityOptions { get; set; } = new();

    public string[] AllowedOrigins { get; set; } = [];

    public string OtpIssuerName { get; set; }

    public bool PreventReuseOfPreviousPasswords { get; set; }

    public string FacebookAppSecret { get; set; }
}