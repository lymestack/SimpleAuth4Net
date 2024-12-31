using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleAuthNet.Models;

[Table("AppUser")]
public class AppUser
{
    public int Id { get; set; }

    public string Username { get; set; }

    public string EmailAddress { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string? PhoneNumber { get; set; }

    public bool? PhoneNumberVerified { get; set; }

    public DateTime DateEntered { get; set; }

    public DateTime? LastSeen { get; set; }

    public bool Verified { get; set; } = false;

    public bool Active { get; set; } = true;

    public bool Locked { get; set; } = false;

    public int? PreferredMfaMethod { get; set; }

    public AppUserCredential? AppUserCredential { get; set; }

    /// <summary>
    /// EF Navigation Property
    /// </summary>
    public IList<AppUserRole>? AppUserRoles { get; set; } = [];

    /// <summary>
    /// EF Navigation Property
    /// </summary>
    public IList<AppRefreshToken>? AppRefreshTokens { get; set; } = [];

    public IList<AppUserPasswordHistory>? AppUserPasswordHistories { get; set; } = [];

    [NotMapped] public IList<string> Roles { get; set; } = [];

    [NotMapped] public Guid SessionId { get; set; }

}
