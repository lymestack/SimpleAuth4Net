using Newtonsoft.Json;

namespace SimpleAuthNet.Models.SsoResponse;

public class FacebookUserInfo
{
    [JsonProperty("first_name")]
    public string FirstName { get; set; }

    [JsonProperty("last_name")]
    public string LastName { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }
}
