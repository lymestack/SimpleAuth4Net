namespace SimpleAuthNet.Models.Config;

public class RateLimitOptions
{
    public int PermitLimit { get; set; } = 5;
    public int WindowInSeconds { get; set; } = 60;
    public int QueueLimit { get; set; } = 2;
    public bool EnableRateLimitRejectionLogging { get; set; }
}
