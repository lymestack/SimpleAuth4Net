using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleAuthNet.Models;

[Table("AppUserCredential")]
public class AppUserCredential
{
    [System.ComponentModel.DataAnnotations.Key]
    public int AppUserId { get; set; }

    public AppUser AppUser { get; set; }

    public byte[] PasswordSalt { get; set; }

    public byte[] PasswordHash { get; set; }

    public DateTime DateCreated { get; set; }

    public string VerifyToken { get; set; }

    public DateTime VerifyTokenExpires { get; set; }

    public bool VerifyTokenUsed { get; set; }
}