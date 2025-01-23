using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SimpleAuthNet.Models;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace SimpleAuthNet.Data;

public class SimpleAuthContext(IConfiguration configuration) : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // connect to sql server with connection string from app settings
        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUserRole>().HasKey(x => new { x.AppUserId, x.AppRoleId });
    }

    public DbSet<AppUser> AppUsers { get; set; }

    public DbSet<AppRole> AppRoles { get; set; }

    public DbSet<AppRefreshToken> AppRefreshTokens { get; set; }

    public DbSet<AppUserPasswordHistory> AppUserPasswordHistories { get; set; }

    #region Adhoc Sql Queries

    public void DeleteRolesForUser(int userId)
    {
        var idParam = new SqlParameter("@userId", userId);
        Database.ExecuteSqlRaw("DELETE FROM AppUserRole WHERE AppUserId = @userId", @idParam);
    }

    public void AddRoleForUser(int userId, string role)
    {
        var idParam = new SqlParameter("@userId", userId);
        var roleParam = new SqlParameter("@role", role);
        Database.ExecuteSqlRaw("INSERT INTO AppUserRole (AppUserId, AppRoleId) VALUES (@userId, (SELECT Id FROM AppRole WHERE Name = @role))", @idParam, roleParam);
    }

    #endregion
}