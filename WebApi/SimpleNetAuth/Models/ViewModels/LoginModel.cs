using System.ComponentModel.DataAnnotations;

namespace SimpleNetAuth.Models.ViewModels;

public class LoginModel
{
    [Required]
    public string Username { get; set; }

    [Required]
    public string Password { get; set; }
}