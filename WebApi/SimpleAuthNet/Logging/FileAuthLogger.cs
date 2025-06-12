using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SimpleAuthNet.Models;

namespace SimpleAuthNet.Logging;

public class FileAuthLogger : IAuthLogger
{
    private readonly string _basePath;

    public FileAuthLogger(IConfiguration configuration)
    {
        _basePath = configuration.GetValue<string>("AuthSettings:AuditLogging:LogFolder")
                    ?? Path.Combine(AppContext.BaseDirectory, "Logs", "SimpleAuth");
    }

    public async Task LogAsync(string logType, object data)
    {
        var now = DateTime.UtcNow;
        var folder = Path.Combine(_basePath, logType, now.Year.ToString(), now.Month.ToString("D2"));
        Directory.CreateDirectory(folder);

        var filename = $"{now:yyyyMMdd_HHmmssfff}.json";
        var filePath = Path.Combine(folder, filename);

        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json);
    }

    public Task LogLoginSuccessAsync(string username, string method) =>
        LogAsync("LoginSuccess", new { Timestamp = DateTime.UtcNow, Username = username, Method = method });

    public Task LogLoginFailureAsync(string username, string reason) =>
        LogAsync("LoginFailure", new { Timestamp = DateTime.UtcNow, Username = username, Reason = reason });

    public Task LogPasswordResetAsync(string username) =>
        LogAsync("PasswordReset", new { Timestamp = DateTime.UtcNow, Username = username });

    public Task LogMfaVerifiedAsync(string username, string method) =>
        LogAsync("MfaVerified", new { Timestamp = DateTime.UtcNow, Username = username, Method = method });

    public Task LogTokenRefreshAsync(string username) =>
        LogAsync("TokenRefresh", new { Timestamp = DateTime.UtcNow, Username = username });

    public Task LogAccountVerifiedAsync(string username) =>
        LogAsync("AccountVerified", new { Timestamp = DateTime.UtcNow, Username = username });

    public Task LogSessionRevokedAsync(string userId, string sessionId, bool isGlobal = false)
    {
        var logData = new
        {
            Timestamp = DateTime.UtcNow,
            UserId = userId,
            SessionId = sessionId,
            IsGlobal = isGlobal,
            Message = isGlobal
                ? "All sessions revoked for user."
                : $"Session {sessionId} revoked for user."
        };

        return LogAsync("SessionRevocation", logData);
    }

    public Task LogRegistrationAsync(string username) =>
        LogAsync("Registration", new { Timestamp = DateTime.UtcNow, Username = username });

}
