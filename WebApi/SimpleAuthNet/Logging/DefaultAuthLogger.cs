using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleAuthNet.Models;
using SimpleAuthNet.Models.Config;

namespace SimpleAuthNet.Logging;

public class DefaultAuthLogger(ILogger<DefaultAuthLogger> logger, IOptions<AuthSettings> options)
    : IAuthLogger
{
    private readonly AuthSettings _settings = options.Value;

    public Task LogAsync(AuthLogEventType eventType, string username, object? data)
    {
        if (!(_settings.AuditLogging?.Enabled ?? false)) return Task.CompletedTask;
        var loggingOptions = _settings.AuditLogging;
        if (loggingOptions.Enabled) WriteLog(LogLevel.Information, username, data);
        return Task.CompletedTask;
    }

    private Task WriteLog(LogLevel level, string username, object? data)
    {
        logger.Log(level, "{Label}: Username={Username}, Data={@Data}", username, data);
        return Task.CompletedTask;
    }
}
