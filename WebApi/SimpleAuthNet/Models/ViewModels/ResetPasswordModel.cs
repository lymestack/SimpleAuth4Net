namespace SimpleAuthNet.Models.ViewModels;

public class ResetPasswordModel
{
    // FUTURE: Add Username to this model.

    public string Token { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}