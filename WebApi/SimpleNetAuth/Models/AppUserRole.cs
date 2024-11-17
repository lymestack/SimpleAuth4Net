using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleNetAuth.Models;

[Table("AppUserRole")]
public class AppUserRole
{
    [Column(Order = 0)]
    public int AppUserId { get; set; }

    public AppUser AppUser { get; set; }

    [Column(Order = 1)]
    public int AppRoleId { get; set; }

    public AppRole AppRole { get; set; }
}