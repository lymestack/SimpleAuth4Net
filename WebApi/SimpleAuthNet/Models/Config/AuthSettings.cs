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

    public string MicrosoftClientSecret { get; set; }

    public AuditLoggingOptions AuditLogging { get; set; } = new();
}

public class AuditLoggingOptions
{
    public bool Enabled { get; set; }
    public bool LogLoginSuccess { get; set; }
    public bool LogLoginFailure { get; set; }
    public bool LogPasswordReset { get; set; }
    public bool LogTokenRefresh { get; set; }
    public bool LogMfaVerification { get; set; }
    public bool LogAccountVerification { get; set; }
    public bool LogUserRegistration { get; set; }
    public bool LogSessionRevocations { get; set; } // <-- add this
}

