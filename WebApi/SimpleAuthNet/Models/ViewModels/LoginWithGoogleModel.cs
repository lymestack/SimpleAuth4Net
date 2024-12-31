namespace SimpleAuthNet.Models.ViewModels;

public class LoginWithGoogleModel
{
    public string CredentialsFromGoogle { get; set; }

    public string DeviceId { get; set; }
}

public class LoginWithFacebookModel
{
    public string CredentialsFromFacebook { get; set; }

    public string DeviceId { get; set; }
}