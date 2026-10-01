using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SimpleAuthNet.Models;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace SimpleAuthNet.Data;

public class SimpleAuthContext(IConfiguration configuration, DbContextOptions<SimpleAuthContext> contextOptions) : DbContext(contextOptions), IRoleDbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // Honor a provider already configured through DbContextOptions (e.g. tests); otherwise
        // connect to sql server with connection string from app settings
        if (options.IsConfigured) return;
        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUserRole>().HasKey(x => new { x.AppUserId, x.AppRoleId });
    }

    public DbSet<AppUser> AppUsers { get; set; }

    public DbSet<AppRole> AppRoles { get; set; }

    public DbSet<AppUserRole> AppUserRoles { get; set; }

    public DbSet<AppRefreshToken> AppRefreshTokens { get; set; }

    public DbSet<AppUserPasswordHistory> AppUserPasswordHistories { get; set; }

    #region Adhoc Sql Queries

    public void DeleteRolesForUser(int userId)
    {
        Database.ExecuteSqlInterpolated($"DELETE FROM AppUserRole WHERE AppUserId = {userId}");
    }

    public void AddRoleForUser(int userId, string role)
    {
        Database.ExecuteSqlInterpolated($"INSERT INTO AppUserRole (AppUserId, AppRoleId) VALUES ({userId}, (SELECT Id FROM AppRole WHERE Name = {role}))");
    }

    #endregion
}