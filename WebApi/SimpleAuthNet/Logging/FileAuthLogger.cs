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

    public async Task LogAsync(AuthLogEventType eventType, string username, object? data)
    {
        var now = DateTime.UtcNow;
        var folder = Path.Combine(_basePath, eventType.ToString(), now.Year.ToString(), now.Month.ToString("D2"));
        Directory.CreateDirectory(folder);

        var filename = $"{now:yyyyMMdd_HHmmssfff}.json";
        var filePath = Path.Combine(folder, filename);

        var logEntry = new
        {
            Timestamp = now,
            EventType = eventType.ToString(),
            Username = username,
            Data = data
        };

        var json = JsonConvert.SerializeObject(logEntry, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json);
    }
}