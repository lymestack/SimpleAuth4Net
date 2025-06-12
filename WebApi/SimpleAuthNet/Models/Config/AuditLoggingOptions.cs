namespace SimpleAuthNet.Models.Config;

public class AuditLoggingOptions
{
    public bool Enabled { get; set; }
    public bool LogLoginSuccess { get; set; }
    public bool LogLoginFailure { get; set; }
    public bool LogPasswordReset { get; set; }
    public bool LogTokenRefresh { get; set; }
    public bool LogMfaVerification { get; set; }
    public bool LogAccountVerification { get; set; }
    public bool LogUserRegistration { get; set; }
    public bool LogSessionRevocations { get; set; } // <-- add this
    public string LogFolder { get; set; } = "";
}