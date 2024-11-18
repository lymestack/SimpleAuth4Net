namespace SimpleAuthNet.Models.Config;

public class AuthSettings
{
    public string GoogleClientId { get; set; }

    public int AccessTokenExpirationMinutes { get; set; }

    public bool UseRefreshTokens { get; set; }

    public int RefreshTokenExpirationDays { get; set; }

    public bool StoreTokensInCookies { get; set; }
}