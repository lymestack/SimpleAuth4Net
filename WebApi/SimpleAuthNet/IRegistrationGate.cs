using Microsoft.Extensions.Configuration;
using SimpleAuthNet.Models.Config;
using SimpleAuthNet.Models.ViewModels;

namespace SimpleAuthNet;

/// <summary>
/// The outcome of an <see cref="IRegistrationGate"/> check. A denial carries a reason for logs and
/// callers; <c>AuthController.Register</c> still returns the fixed "Public registration is not allowed." text.
/// </summary>
public readonly record struct RegistrationDecision(bool Allowed, string? Reason = null)
{
    public static RegistrationDecision Allow() => new(true);
    public static RegistrationDecision Deny(string reason) => new(false, reason);
}

/// <summary>
/// Decides whether <c>Auth/Register</c> may proceed for a given request. The default
/// implementation reads <c>AppConfig:SimpleAuth:AllowRegistration</c>; an app or core layer can replace
/// it (for example to admit holders of a valid invite token while public registration is off).
/// </summary>
public interface IRegistrationGate
{
    Task<RegistrationDecision> EvaluateAsync(RegisterModel model);
}

/// <summary>Allows registration when <c>AllowRegistration</c> is true, otherwise denies.</summary>
public class DefaultRegistrationGate(IConfiguration configuration) : IRegistrationGate
{
    public Task<RegistrationDecision> EvaluateAsync(RegisterModel model)
    {
        var settings = configuration.GetSection("AppConfig:SimpleAuth").Get<SimpleAuthSettings>();
        return Task.FromResult(settings?.AllowRegistration == true
            ? RegistrationDecision.Allow()
            : RegistrationDecision.Deny("Public registration is not allowed."));
    }
}
