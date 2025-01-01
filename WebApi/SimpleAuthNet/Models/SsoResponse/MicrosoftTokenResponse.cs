using Newtonsoft.Json;

namespace SimpleAuthNet.Models.SsoResponse;

public class MicrosoftTokenResponse
{
    [JsonProperty("token_type")]
    public string TokenType { get; set; }

    [JsonProperty("scope")]
    public string Scope { get; set; }

    [JsonProperty("expires_in")]
    public long ExpiresIn { get; set; }

    [JsonProperty("ext_expires_in")]
    public long ExtExpiresIn { get; set; }

    [JsonProperty("access_token")]
    public string AccessToken { get; set; }

    [JsonProperty("refresh_token")]
    public string RefreshToken { get; set; }
}