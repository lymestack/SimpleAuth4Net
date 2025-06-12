namespace SimpleAuthNet.Models;

public enum AuthLogEventType
{
    LoginSuccess,
    LoginFailure,
    PasswordReset,
    TokenRefresh,
    MfaVerified,
    Registration,
    AccountVerified,
    SessionRevoked,
    Logout
}

public interface IAuthLogger
{
    Task LogAsync(AuthLogEventType eventType, string username, object? data);
}

