using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using SimpleAuthNet.Data;
using SimpleAuthNet.Models;

namespace WebApi.Tests;

/// <summary>
/// Hosts the real API pipeline (authorization filters, JWT, rate limiting) over a throwaway
/// in-memory SQLite database, so no SQL Server is needed and nothing real is touched.
/// </summary>
public class AuthApiFactory : WebApplicationFactory<WebApi.Controllers.AuthController>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Testing");
        // Keep the fixed-window limiter out of the way. Audit logging is off, which also proves
        // AuthController resolves an IAuthLogger (the default no-op) without it being enabled.
        builder.UseSetting("AuthSettings:RateLimit:PermitLimit", "100000");
        builder.UseSetting("AuthSettings:AuditLogging:Enabled", "false");

        builder.ConfigureServices(services =>
        {
            services.AddDbContext<SimpleAuthContext>(o => o.UseSqlite(_connection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SimpleAuthContext>();
        // The real schema (CreateDb.sql) allows a NULL VerifyToken; the non-nullable model property
        // would make EF generate NOT NULL, so relax it in the generated SQLite script.
        var script = db.Database.GenerateCreateScript().Replace("\"VerifyToken\" TEXT NOT NULL", "\"VerifyToken\" TEXT");
        db.Database.ExecuteSqlRaw(script);
        // SimpleAuthContext.DeleteRolesForUser / AddRoleForUser use the SQL Server table name.
        db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS AppUserRole (AppUserId INTEGER NOT NULL, AppRoleId INTEGER NOT NULL)");
        return host;
    }

    public HttpClient CreateClientFor(params string[] roles)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (roles.Length > 0)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("tester", roles));
        return client;
    }

    public string CreateToken(string username, params string[] roles)
    {
        var secret = Services.GetRequiredService<IConfiguration>()["AuthSettings:TokenSecret"]!;
        var claims = new List<Claim> { new(ClaimTypes.Name, username), new("sub", "1") };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha512));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<AppUser> AddUserAsync(string username, string? email)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SimpleAuthContext>();
        var user = new AppUser
        {
            Username = username,
            EmailAddress = email,
            FirstName = "Test",
            LastName = "User",
            DateEntered = DateTime.UtcNow
        };
        db.AppUsers.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
