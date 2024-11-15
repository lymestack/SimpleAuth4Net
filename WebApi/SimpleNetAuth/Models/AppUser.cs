using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleNetAuth.Models;

[Table("AppUser")]
public class AppUser
{
    public int Id { get; set; }

    public string Username { get; set; }

    public string EmailAddress { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public DateTime DateEntered { get; set; }

    public DateTime? LastSeen { get; set; }

    public AppUserCredential? AppUserCredential { get; set; } = null;

    /// <summary>
    /// EF Navigation Property
    /// </summary>
    public IList<AppUserRole>? AppUserRoles { get; set; }

    /// <summary>
    /// EF Navigation Property
    /// </summary>
    public IList<AppRefreshToken> AppRefreshTokens { get; set; }
}
