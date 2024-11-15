using System.ComponentModel.DataAnnotations;

namespace SimpleNetAuth.Models.ViewModels;

public class RegisterModel
{
    [Required]
    public string Username { get; set; }

    [Required]
    public string Password { get; set; }

    [Required]
    public string ConfirmPassword { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string EmailAddress { get; set; }
}