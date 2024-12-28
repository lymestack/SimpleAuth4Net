namespace SimpleAuthNet.Models.ViewModels;

public class VerifyTotpModel
{
    public string Username { get; set; }
    public string Code { get; set; }
    public string DeviceId { get; set; }
}