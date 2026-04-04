using Microsoft.EntityFrameworkCore;
using SimpleAuthNet.Models;

namespace SimpleAuthNet.Data;

public interface IRoleDbContext
{
    DbSet<AppUserRole> AppUserRoles { get; }
    DbSet<AppRole> AppRoles { get; }
}
