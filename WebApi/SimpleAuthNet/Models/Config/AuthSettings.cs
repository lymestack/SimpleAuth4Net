namespace SimpleAuthNet.Models.Config;

public class AuthSettings
{
    public string TokenSecret { get; set; }

    public int AccessTokenExpirationMinutes { get; set; }

    public bool UseRefreshTokens { get; set; }

    public int RefreshTokenExpirationDays { get; set; }

    public bool StoreTokensInCookies { get; set; }

    public bool RequireUserVerification { get; set; }

    public PasswordComplexityOptions PasswordComplexityOptions { get; set; } = new();
}