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

    public AuditLoggingOptions AuditLogging { get; set; } = new();

    public List<SsoProviderSettings> SsoProviders { get; set; } = new();

    public SimpleAuthMode Mode { get; set; } = SimpleAuthMode.Standalone;

    public string IdentityProviderUrl { get; set; } = "";

    public string? CookieDomain { get; set; }

    public string ReturnUrlParameter { get; set; } = "returnUrl";

    /// <summary>
    /// Forces auth cookies to be issued with the Secure flag regardless of <c>Request.IsHttps</c>.
    /// Set to true in production when TLS is terminated at a reverse proxy / load balancer so that
    /// cookies are never sent without Secure even though Kestrel sees plain HTTP.
    /// Leave false for local HTTP development.
    /// </summary>
    public bool AlwaysUseSecureCookies { get; set; }

    /// <summary>
    /// Expected token issuer. When set, tokens are stamped with this <c>iss</c> value and the JWT
    /// bearer validates it (ValidateIssuer = true). Leave empty to disable issuer validation
    /// (default for Standalone deployments). In SSO deployments set the same value on the
    /// IdentityProvider and every RelyingApp.
    /// </summary>
    public string TokenIssuer { get; set; } = "";

    /// <summary>
    /// Expected token audience. When set, tokens are stamped with this <c>aud</c> value and the JWT
    /// bearer validates it (ValidateAudience = true). Leave empty to disable audience validation.
    /// In SSO deployments this scopes which relying app(s) a token is intended for.
    /// </summary>
    public string TokenAudience { get; set; } = "";
}