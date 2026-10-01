using System.Net;

namespace WebApi.Tests;

// Drives the real pipeline (JWT, global authorization filter, role checks) over in-memory SQLite.
public class AuthEndpointSecurityTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    public static TheoryData<string, string> AdminEndpoints => new()
    {
        { "POST", "/Auth/UnlockUser?username=nobody" },
        { "POST", "/Auth/RevokeAllSessionsForUser?username=nobody" },
        { "POST", "/Auth/RevokeAllSessions" },
        { "GET", "/Auth/EmailExists?email=a@example.com" },
    };

    private static HttpRequestMessage Request(string method, string url) => new(new HttpMethod(method), url);

    [Theory]
    [MemberData(nameof(AdminEndpoints))]
    public async Task AdminEndpoint_Anonymous_Returns401(string method, string url)
    {
        var response = await factory.CreateClientFor().SendAsync(Request(method, url));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AdminEndpoints))]
    public async Task AdminEndpoint_NonAdmin_Returns403(string method, string url)
    {
        var response = await factory.CreateClientFor("User").SendAsync(Request(method, url));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AdminEndpoints))]
    public async Task AdminEndpoint_Admin_PassesAuthorization(string method, string url)
    {
        var response = await factory.CreateClientFor("Admin").SendAsync(Request(method, url));
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SetupAuthenticator_Anonymous_Returns401()
    {
        var response = await factory.CreateClientFor().SendAsync(Request("POST", "/Auth/SetupAuthenticator?username=nobody"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/Auth/Login")]
    [InlineData("POST", "/Auth/ForgotPassword")]
    [InlineData("POST", "/Auth/RefreshToken")]
    public async Task PublicEndpoint_Anonymous_IsReachable(string method, string url)
    {
        var request = Request(method, url);
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var response = await factory.CreateClientFor().SendAsync(request);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
