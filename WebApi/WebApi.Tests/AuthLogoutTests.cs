using System.Net;
using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SimpleAuthNet.Data;
using SimpleAuthNet.Models;

namespace WebApi.Tests;

/// <summary><c>DELETE Auth/Logout</c> revokes the presented refresh token server-side, not only the cookie.</summary>
public class AuthLogoutTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private static string Hash(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private async Task<(int UserId, string Raw)> SeedRefreshTokenAsync(string deviceId)
    {
        var user = await factory.AddUserAsync($"logout-{Guid.NewGuid():N}@example.test", null);
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SimpleAuthContext>();
        db.AppRefreshTokens.Add(new AppRefreshToken
        {
            AppUserId = user.Id,
            DeviceId = deviceId,
            Token = Hash(raw),
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(7)
        });
        await db.SaveChangesAsync();
        return (user.Id, raw);
    }

    private async Task<int> TokenRowsAsync(int userId)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SimpleAuthContext>().AppRefreshTokens.CountAsync(t => t.AppUserId == userId);
    }

    private HttpRequestMessage WithRefreshCookie(HttpMethod method, string url, string raw)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Cookie", $"X-Refresh-Token={Uri.EscapeDataString(raw)}");
        return request;
    }

    [Fact]
    public async Task Logout_DeletesThePresentedRefreshToken_SoItCannotRefreshAgain()
    {
        const string device = "logout-device";
        var (userId, raw) = await SeedRefreshTokenAsync(device);
        var other = await SeedRefreshTokenAsync("other-device");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });

        using (var logout = await client.SendAsync(WithRefreshCookie(HttpMethod.Delete, "/Auth/Logout", raw)))
            Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        Assert.Equal(0, await TokenRowsAsync(userId));
        Assert.Equal(1, await TokenRowsAsync(other.UserId)); // only the presented token goes
        using var refresh = await client.SendAsync(WithRefreshCookie(HttpMethod.Get, $"/Auth/RefreshToken?deviceId={device}", raw));
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        // Control: the untouched token still refreshes, so the 401 above is the revocation.
        using var otherRefresh = await client.SendAsync(WithRefreshCookie(HttpMethod.Get, "/Auth/RefreshToken?deviceId=other-device", other.Raw));
        Assert.Equal(HttpStatusCode.OK, otherRefresh.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutARefreshCookie_StillSucceeds()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var logout = await client.DeleteAsync("/Auth/Logout");
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
    }
}
