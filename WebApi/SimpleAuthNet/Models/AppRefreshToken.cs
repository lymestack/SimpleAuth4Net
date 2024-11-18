using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleAuthNet.Models;

[Table("AppRefreshToken")]
public class AppRefreshToken
{
    [System.ComponentModel.DataAnnotations.Key]
    [ForeignKey("AppUser")]
    public int AppUserId { get; set; }

    public string Token { get; set; }

    public DateTime Created { get; set; }

    public DateTime Expires { get; set; }

    public AppUser? AppUser { get; set; }
}