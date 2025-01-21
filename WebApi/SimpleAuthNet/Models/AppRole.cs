using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleAuthNet.Models;

[Table("AppRole")]
public class AppRole
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// EF Navigation Property
    /// </summary>
    public IList<AppUserRole>? AppUserRoles { get; set; }
}
