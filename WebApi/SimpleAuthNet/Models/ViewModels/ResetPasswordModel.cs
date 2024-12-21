namespace SimpleAuthNet.Models.ViewModels;

public class ResetPasswordModel
{
    public string Username { get; set; } = string.Empty;

    public string VerifyToken { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}

public class VerifyIdentityModel
{
    public string Username { get; set; } = string.Empty;

    public string VerifyToken { get; set; } = string.Empty;

    public string? DeviceId { get; set; }
}