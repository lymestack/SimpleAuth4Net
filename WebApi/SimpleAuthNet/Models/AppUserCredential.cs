using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleAuthNet.Models;

[Table("AppUserCredential")]
public class AppUserCredential
{
    [System.ComponentModel.DataAnnotations.Key]
    public int AppUserId { get; set; }

    public AppUser AppUser { get; set; }

    public byte[]? PasswordSalt { get; set; }

    public byte[]? PasswordHash { get; set; }

    public DateTime DateCreated { get; set; }

    public string VerifyToken { get; set; }

    public DateTime VerifyTokenExpires { get; set; }

    public bool VerifyTokenUsed { get; set; }

    public bool PendingMfaLogin { get; set; }

    public int FailedLoginAttempts { get; set; } = 0;

    public DateTime? LastFailedLoginAttempt { get; set; }

    public DateTime? LockoutEndTime { get; set; }

    public string? TotpSecret { get; set; }

    public DateTime? VerificationCooldownExpires { get; set; }
}