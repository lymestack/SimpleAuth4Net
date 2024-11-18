using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SimpleAuthNet.Models;

namespace SimpleAuthNet.Data;

public class SimpleAuthNetDataContext(IConfiguration configuration) : DbContext
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

    //#region Adhoc Sql Queries

    //public void DeleteRolesForUser(int userId)
    //{
    //    var idParam = new SqlParameter("@userId", userId);
    //    Database.ExecuteSqlRaw("DELETE FROM UserInfo_AppRole WHERE UserInfoId = @userId", @idParam);
    //}

    //public void AddRoleForUser(int userId, string role)
    //{
    //    var idParam = new SqlParameter("@userId", userId);
    //    var roleParam = new SqlParameter("@role", role);
    //    Database.ExecuteSqlRaw("INSERT INTO AppUserRoles (AppUserId, AppRoleId) VALUES (@userId, (SELECT Id FROM AppRole WHERE Name = @role))", @idParam, roleParam);
    //}

    //#endregion
}