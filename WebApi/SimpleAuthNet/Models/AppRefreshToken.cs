using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleAuthNet.Models;

[Table("AppRefreshToken")]
public class AppRefreshToken
{
    public int Id { get; set; }

    public int AppUserId { get; set; }

    public AppUser? AppUser { get; set; }

    public string DeviceId { get; set; }

    public string Token { get; set; }

    public DateTime Created { get; set; }

    public DateTime Expires { get; set; }
}