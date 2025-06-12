namespace SimpleAuthNet.Models;

public interface IAuthLogger
{
    Task LogLoginSuccessAsync(string userId, string method);
    Task LogLoginFailureAsync(string userIdOrUsername, string reason);
    Task LogPasswordResetAsync(string userId);
    Task LogTokenRefreshAsync(string userId);
    Task LogMfaVerifiedAsync(string userId, string method);
    Task LogRegistrationAsync(string userId);
    Task LogAccountVerifiedAsync(string userId);
    Task LogSessionRevokedAsync(string userId, string sessionId, bool isGlobal = false);

    // Task LogAdminActionAsync(string adminUserId, string action, string targetUserId);
}
