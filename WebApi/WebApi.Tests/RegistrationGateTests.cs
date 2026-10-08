using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleAuthNet;
using SimpleAuthNet.Data;
using SimpleAuthNet.Models;
using SimpleAuthNet.Models.ViewModels;

namespace WebApi.Tests;

public class StubRegistrationGate(bool allow) : IRegistrationGate
{
    public Task<RegistrationDecision> EvaluateAsync(RegisterModel model) =>
        Task.FromResult(allow ? RegistrationDecision.Allow() : RegistrationDecision.Deny("stub"));
}

/// <summary>Hosts the API with AllowRegistration forced to a value and, optionally, a stub gate replacing the default.</summary>
public abstract class GateFactory(bool allowRegistration, bool? stubAllows) : AuthApiFactory
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SimpleAuthContext>();
        db.AppRoles.Add(new AppRole { Name = "Admin" }); // Register grants this to the first user
        db.SaveChanges();
        return host;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("AppConfig:SimpleAuth:AllowRegistration", allowRegistration ? "true" : "false");
        if (stubAllows is { } allow)
            builder.ConfigureServices(services => services.AddScoped<IRegistrationGate>(_ => new StubRegistrationGate(allow)));
    }
}

public class RegistrationGateTests
{
    private static RegisterModel Model(string username) => new()
    {
        Username = username, EmailAddress = username, Password = "Str0ng!Passw0rd", ConfirmPassword = "Str0ng!Passw0rd",
        FirstName = "Reg", LastName = "Istrant"
    };

    private static Task<HttpResponseMessage> Register(AuthApiFactory factory, string username) =>
        factory.CreateClientFor().PostAsJsonAsync("/Auth/Register", Model(username));

    private static async Task AssertDenied(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Public registration is not allowed.", await response.Content.ReadAsStringAsync());
    }

    private static DefaultRegistrationGate GateWithFlag(bool? flag)
    {
        var values = new Dictionary<string, string?>();
        if (flag is { } f) values["AppConfig:SimpleAuth:AllowRegistration"] = f.ToString();
        return new DefaultRegistrationGate(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(null, false)]
    public async Task DefaultGate_FollowsAllowRegistrationFlag(bool? flag, bool expectedAllowed)
    {
        var decision = await GateWithFlag(flag).EvaluateAsync(Model("unit@example.com"));

        Assert.Equal(expectedAllowed, decision.Allowed);
        if (!expectedAllowed) Assert.False(string.IsNullOrEmpty(decision.Reason));
    }

    public class FlagFalseFactory() : GateFactory(false, null);
    public class FlagTrueFactory() : GateFactory(true, null);
    public class StubAllowsFlagFalseFactory() : GateFactory(false, true);
    public class StubDeniesFlagTrueFactory() : GateFactory(true, false);

    public class DefaultGateFlagFalse(FlagFalseFactory factory) : IClassFixture<FlagFalseFactory>
    {
        [Fact]
        public async Task Register_IsBlocked() => await AssertDenied(await Register(factory, "blocked@example.com"));
    }

    public class DefaultGateFlagTrue(FlagTrueFactory factory) : IClassFixture<FlagTrueFactory>
    {
        [Fact]
        public async Task Register_Succeeds()
        {
            var response = await Register(factory, "allowed@example.com");
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        }
    }

    public class StubAllowsOverridesFalseFlag(StubAllowsFlagFalseFactory factory) : IClassFixture<StubAllowsFlagFalseFactory>
    {
        [Fact]
        public async Task Register_Succeeds()
        {
            var response = await Register(factory, "stub-allows@example.com");
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        }
    }

    public class StubDeniesOverridesTrueFlag(StubDeniesFlagTrueFactory factory) : IClassFixture<StubDeniesFlagTrueFactory>
    {
        [Fact]
        public async Task Register_IsBlocked() => await AssertDenied(await Register(factory, "stub-denies@example.com"));
    }
}
