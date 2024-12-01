namespace SimpleAuthNet.Models.ViewModels;

public class ResetPasswordModel
{
    public string Token { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}