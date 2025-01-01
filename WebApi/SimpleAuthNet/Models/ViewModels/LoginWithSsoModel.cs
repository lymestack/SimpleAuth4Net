namespace SimpleAuthNet.Models.ViewModels;

public class LoginWithSsoModel
{
    public SsoProvider SsoProvider { get; set; }

    public string DeviceId { get; set; }

    public string CredentialsFromProvider { get; set; }
}
