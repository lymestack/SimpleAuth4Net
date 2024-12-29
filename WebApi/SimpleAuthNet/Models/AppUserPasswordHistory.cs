using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleAuthNet.Models;

[Table("AppUserPasswordHistory")]
public class AppUserPasswordHistory
{
    public int Id { get; set; }

    public int AppUserId { get; set; }

    public byte[] HashedPassword { get; set; }

    public byte[] Salt { get; set; }

    public DateTime DateCreated { get; set; }

    public AppUser AppUser { get; set; }
}
