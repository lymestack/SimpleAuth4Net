using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleNetAuth.Models;

[Table("AppUserCredential")]
public class AppUserCredential
{
    [System.ComponentModel.DataAnnotations.Key]
    public int AppUserId { get; set; }

    public AppUser AppUser { get; set; }

    public byte[] PasswordSalt { get; set; }

    public byte[] PasswordHash { get; set; }

    public DateTime DateCreated { get; set; }
}