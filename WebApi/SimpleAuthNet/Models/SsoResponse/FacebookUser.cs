using Newtonsoft.Json;

namespace SimpleAuthNet.Models.SsoResponse;

public class FacebookUser
{
    [JsonProperty("data")]
    public FacebookUserData FacebookUserData { get; set; }
}

public class FacebookUserData
{
    [JsonProperty("app_id")]
    public string AppId { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("application")]
    public string Application { get; set; }

    [JsonProperty("data_access_expires_at")]
    public long DataAccessExpiresAt { get; set; }

    [JsonProperty("expires_at")]
    public long ExpiresAt { get; set; }

    [JsonProperty("is_valid")]
    public bool IsValid { get; set; }

    [JsonProperty("scopes")]
    public List<string> Scopes { get; set; }

    [JsonProperty("user_id")]
    public string UserId { get; set; }
}
