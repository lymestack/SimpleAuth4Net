using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleAuthNet.Models;
using SimpleAuthNet.Models.Config;

namespace SimpleAuthNet.Logging;

public class DefaultAuthLogger(ILogger<DefaultAuthLogger> logger, IOptions<AuthSettings> options)
    : IAuthLogger
{
    private readonly AuthSettings _settings = options.Value;

    private bool IsEnabled(string propertyName) =>
        _settings.AuditLogging?.Enabled == true &&
        typeof(AuditLoggingOptions)
            .GetProperty(propertyName)?
            .GetValue(_settings.AuditLogging) as bool? == true;

    public Task LogLoginSuccessAsync(string userId, string method)
    {
        if (!IsEnabled(nameof(AuditLoggingOptions.LogLoginSuccess))) return Task.CompletedTask;
        logger.LogInformation("LOGIN SUCCESS: User {UserId} via {Method}", userId, method);
        return Task.CompletedTask;
    }

    public Task LogLoginFailureAsync(string userIdOrUsername, string reason)
    {
        if (!IsEnabled(nameof(AuditLoggingOptions.LogLoginFailure))) return Task.CompletedTask;
        logger.LogWarning("LOGIN FAILURE: {User} failed to login. Reason: {Reason}", userIdOrUsername, reason);
        return Task.CompletedTask;
    }

    public Task LogPasswordResetAsync(string userId)
    {
        if (!IsEnabled(nameof(AuditLoggingOptions.LogPasswordReset))) return Task.CompletedTask;
        logger.LogInformation("PASSWORD RESET: User {UserId}", userId);
        return Task.CompletedTask;
    }

    public Task LogTokenRefreshAsync(string userId)
    {
        if (!IsEnabled(nameof(AuditLoggingOptions.LogTokenRefresh))) return Task.CompletedTask;
        logger.LogInformation("TOKEN REFRESH: User {UserId}", userId);
        return Task.CompletedTask;
    }

    public Task LogMfaVerifiedAsync(string userId, string method)
    {
        if (!IsEnabled(nameof(AuditLoggingOptions.LogMfaVerification))) return Task.CompletedTask;
        logger.LogInformation("MFA VERIFIED: User {UserId} via {Method}", userId, method);
        return Task.CompletedTask;
    }

    public Task LogRegistrationAsync(string userId)
    {
        if (!IsEnabled(nameof(AuditLoggingOptions.LogUserRegistration))) return Task.CompletedTask;
        logger.LogInformation("REGISTRATION: New user registered: {UserId}", userId);
        return Task.CompletedTask;
    }

    public Task LogAccountVerifiedAsync(string userId)
    {
        if (!IsEnabled(nameof(AuditLoggingOptions.LogAccountVerification))) return Task.CompletedTask;
        logger.LogInformation("ACCOUNT VERIFIED: User {UserId}", userId);
        return Task.CompletedTask;
    }

    public Task LogSessionRevokedAsync(string userId, string sessionId, bool isGlobal = false)
    {
        if (!IsEnabled(nameof(AuditLoggingOptions.LogSessionRevocations))) return Task.CompletedTask;

        if (isGlobal)
            logger.LogInformation("SESSION REVOCATION (ALL): All sessions revoked for user {UserId}", userId);
        else
            logger.LogInformation("SESSION REVOCATION: Session {SessionId} revoked for user {UserId}", sessionId, userId);

        return Task.CompletedTask;
    }
}
