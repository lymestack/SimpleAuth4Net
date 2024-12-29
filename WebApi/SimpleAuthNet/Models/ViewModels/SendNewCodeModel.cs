namespace SimpleAuthNet.Models.ViewModels;

public class SendNewCodeModel
{
    public string Username { get; set; }
    public MfaMethod MfaMethod { get; set; } // Enum: Email or SMS
}