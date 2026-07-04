using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleAuthNet.Models;

[Table("AppRefreshToken")]
public class AppRefreshToken
{
    public int Id { get; set; }

    public int AppUserId { get; set; }

    public AppUser AppUser { get; set; }

    public string DeviceId { get; set; } = "";

    public string Token { get; set; } = "";

    /// <summary>
    /// Hash of the immediately-preceding (now-consumed) refresh token for this device. Used for
    /// rotation reuse detection: if a token matching this value is presented, it was already rotated
    /// out, which signals theft/replay — the whole token family for the user is then revoked.
    /// </summary>
    public string? PreviousToken { get; set; }

    public DateTime Created { get; set; }

    public DateTime Expires { get; set; }
}