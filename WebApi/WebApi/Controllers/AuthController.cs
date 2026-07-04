using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using OtpNet;
using QRCoder;
using SimpleAuthNet;
using SimpleAuthNet.Data;
using SimpleAuthNet.Logging;
using SimpleAuthNet.Models;
using SimpleAuthNet.Models.Config;
using SimpleAuthNet.Models.SsoResponse;
using SimpleAuthNet.Models.ViewModels;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace WebApi.Controllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class AuthController(
    IConfiguration configuration,
    SimpleAuthContext db,
    HttpClient httpClient,
    IAuthLogger logger,
    ISimpleAuthEmailSender emailSender) : ControllerBase
{
    private readonly AuthSettings _authSettings = configuration.GetSection("AuthSettings").Get<AuthSettings>()!;
    private readonly SimpleAuthSettings _simpleAuthSettings = configuration.GetSection("AppConfig:SimpleAuth").Get<SimpleAuthSettings>()!;

    // Precomputed once per process. Used to equalize login timing when a username doesn't exist so a
    // missing account can't be distinguished from a wrong password by response time (anti-enumeration).
    private static readonly byte[] DummyPasswordHash =
        SimpleAuthPasswordHasher.HashPassword("SimpleAuth::login-timing-equalizer::not-a-real-password").hash;

    #region Register

    [HttpPost("Register")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (!_simpleAuthSettings.EnableLocalAccounts) return BadRequest("Local Accounts are not enabled.");
        if (!_simpleAuthSettings.AllowRegistration) return BadRequest("Public registration is not allowed.");
        
        // Check for duplicate username
        if (await db.AppUsers.AnyAsync(x => x.Username == model.Username))
        {
            return BadRequest(new { error = "USERNAME_EXISTS", message = "A user with this username already exists." });
        }
        
        // Check for duplicate email if different from username
        if (!string.IsNullOrEmpty(model.Username) && await db.AppUsers.AnyAsync(x => x.EmailAddress == model.Username))
        {
            return BadRequest(new { error = "EMAIL_EXISTS", message = "A user with this email address already exists." });
        }

        var userCount = await db.AppUsers.CountAsync();
        var user = new AppUser
        {
            Username = model.Username,
            AppUserCredential = new AppUserCredential(),
            DateEntered = DateTime.UtcNow,
            EmailAddress = model.Username,
            FirstName = model.FirstName,
            LastName = model.LastName
        };

        if (model.ConfirmPassword == model.Password)
        {
            var validator = new PasswordComplexityValidator(_authSettings.PasswordComplexityOptions);
            var result = validator.Validate(model.Password);
            if (!result.Succeeded) return BadRequest(new { success = false, errors = result.Errors });

            var (hash, salt) = SimpleAuthPasswordHasher.HashPassword(model.Password);
            user.AppUserCredential.PasswordSalt = salt;
            user.AppUserCredential.PasswordHash = hash;
            user.AppUserCredential.DateCreated = DateTime.UtcNow;
            user.AppUserCredential.VerifyTokenExpires = DateTime.UtcNow;
        }
        else
        {
            return BadRequest("Passwords don't match");
        }

        await db.AppUsers.AddAsync(user);
        
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Handle database constraint violations
            if (ex.InnerException?.Message.Contains("IX_AppUser_Username") == true)
            {
                return BadRequest(new { error = "USERNAME_EXISTS", message = "A user with this username already exists." });
            }
            if (ex.InnerException?.Message.Contains("IX_AppUser_Email") == true)
            {
                return BadRequest(new { error = "EMAIL_EXISTS", message = "A user with this email address already exists." });
            }
            throw; // Re-throw if it's a different error
        }

        var message = "User Registered Successfully";

        if (_simpleAuthSettings.RequireUserVerification)
        {
            var verifyToken = await SetupVerifyToken(user);
            await SendVerificationEmail(user.EmailAddress, verifyToken, "Verify your email address");
            if (configuration["AppConfig:Environment:Name"]!.Contains("Local")) message += $" Development ONLY: {verifyToken}";
        }

        // If this is the first user in the system, grant admin access:
        if (userCount == 0)
        {
            var adminRole = await db.AppRoles.FirstAsync(r => r.Name == "Admin");
            user.AppUserRoles.Add(new AppUserRole { AppUserId = user.Id, AppRoleId = adminRole.Id });
            await db.SaveChangesAsync();
        }

        // Invoke post-registration handler if registered
        var postRegistrationHandler = HttpContext.RequestServices.GetService<IPostRegistrationHandler>();
        if (postRegistrationHandler != null)
            await postRegistrationHandler.HandleAsync(user, userCount == 0);

        await logger.LogAsync(AuthLogEventType.Registration, user.Username, null);
        return Ok(new { success = true, message });
    }

    #endregion

    #region UserExists

    [HttpGet("UserExists")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> UserExists([FromQuery] string username)
    {
        if (string.IsNullOrEmpty(username)) return BadRequest("Username must be provided.");
        var appUser = await db.AppUsers.SingleOrDefaultAsync(x => x.Username == username);
        var exists = appUser != null;
        return Ok(new { exists });
    }

    #endregion

    #region Login Endpoints - Login / LoginWithGoogle / LoginWithFacebook / LoginWithMicrosoft

    [HttpPost("Login")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        if (!_simpleAuthSettings.EnableLocalAccounts) return NotFound("Local Accounts are not enabled.");

        var user = await GetUserWithCredentialsAndRoles(model.Username);
        if (user == null)
        {
            // Anti-enumeration: run an equivalent Argon2id verification against a dummy hash so a
            // non-existent account costs about the same wall-clock time as a wrong password on a real
            // one. Without this, "no such user" returns instantly and reveals the account is missing.
            // (Residual timing skew remains for un-migrated legacy HMAC rows, which verify faster.)
            SimpleAuthPasswordHasher.Verify(model.Password, DummyPasswordHash, null);
            return BadRequest(new { error = "INVALID_CREDENTIALS", message = "AppUser or password was invalid." });
        }

        var passwordCheck = CheckPassword(model.Password, user);
        var match = passwordCheck.Verified;

        if (!match)
        {
            user.AppUserCredential.FailedLoginAttempts++;
            user.AppUserCredential.LastFailedLoginAttempt = DateTime.UtcNow;
            await logger.LogAsync(AuthLogEventType.LoginFailure, user.Username, null);

            if (user.AppUserCredential.FailedLoginAttempts >= _authSettings.MaxFailedLoginAttempts)
            {
                user.Locked = true;
                user.AppUserCredential.LockoutEndTime = _authSettings.AccountLockoutDurationInMinutes > 0
                    ? DateTime.UtcNow.AddMinutes(_authSettings.AccountLockoutDurationInMinutes)
                    : null;

                await db.SaveChangesAsync();
                return Unauthorized("The account has been locked due to multiple failed login attempts.");
            }

            await db.SaveChangesAsync();
            return BadRequest(new { error = "INVALID_CREDENTIALS", message = "AppUser or password was invalid." });
        }

        // Password is correct — only now enforce account state. Revealing inactive/locked/unverified
        // is safe here because the caller has proven knowledge of the password; an anonymous
        // enumerator (wrong password / no such user) only ever sees the generic INVALID_CREDENTIALS.
        var statusResult = await CheckAccountStatus(user);
        if (statusResult != null) return statusResult;

        // Reset failed login attempts on successful login
        user.AppUserCredential.FailedLoginAttempts = 0;
        user.AppUserCredential.LastFailedLoginAttempt = null;

        // Rehash-on-login migration: if the stored hash is legacy HMAC-SHA512 (or uses outdated
        // Argon2id parameters), transparently upgrade it to the current Argon2id scheme now that we
        // hold the verified plaintext. No forced reset — same password, stronger storage.
        if (passwordCheck.NeedsRehash)
        {
            var (rehash, resalt) = SimpleAuthPasswordHasher.HashPassword(model.Password);
            user.AppUserCredential.PasswordSalt = resalt;
            user.AppUserCredential.PasswordHash = rehash;
        }

        await db.SaveChangesAsync();

        // Multi-factor authentication gate — do not issue a token until a second factor is satisfied.
        var otpEnrolled = _simpleAuthSettings.EnableMfaViaOtp && !string.IsNullOrEmpty(user.AppUserCredential.TotpSecret);
        var emailOrSmsMfa = _simpleAuthSettings.EnableMfaViaEmail || _simpleAuthSettings.EnableMfaViaSms;

        // OTP is enforced when it is the requested method, or when it is the only available second
        // factor (prevents the client defaulting MfaMethod to Email and skipping OTP entirely).
        if (otpEnrolled && (model.MfaMethod == MfaMethod.Otp || !emailOrSmsMfa))
        {
            // Nothing to send — the user reads the code from their authenticator app and completes
            // login via VerifyAuthenticatorCode, which is gated on this PendingMfaLogin flag.
            user.AppUserCredential.PendingMfaLogin = true;
            user.AppUserCredential.FailedVerificationAttempts = 0;
            await db.SaveChangesAsync();

            return Ok(new
            {
                mfaRequired = true,
                redirectUrl = "/account/verify-mfa-otp",
                message = "Enter the code from your authenticator app."
            });
        }

        if (emailOrSmsMfa)
        {
            var verifyToken = await SetupVerifyToken(user, true);
            var message = "A verification code has been sent to your email";
            if (configuration["AppConfig:Environment:Name"]!.Contains("Local")) message += $" Development ONLY: {verifyToken}";

            if (model.MfaMethod == MfaMethod.Email) await SendVerificationEmail(user.EmailAddress, verifyToken, "MFA Verification Code");
            if (model.MfaMethod == MfaMethod.Sms) await SendVerificationSms(user.PhoneNumber, verifyToken);

            user.AppUserCredential.VerificationCooldownExpires = DateTime.UtcNow.AddSeconds(_simpleAuthSettings.ResendCodeDelaySeconds);
            await db.SaveChangesAsync();

            return Ok(new
            {
                mfaRequired = true,
                redirectUrl = "/account/verify-account",
                message
            });
        }

        var jwt = await JwtGenerator(user, model.DeviceId);
        await logger.LogAsync(AuthLogEventType.LoginSuccess, user.Username, model.DeviceId);

        // If there's a return URL (from RelyingApp redirect), validate and include it in the response
        var returnUrl = HttpContext.Request.Query[_authSettings.ReturnUrlParameter].FirstOrDefault();
        if (!string.IsNullOrEmpty(returnUrl))
        {
            // Validate return URL against allowed domains to prevent open redirect
            if (IsAllowedReturnUrl(returnUrl))
            {
                return Ok(new { token = ((dynamic)jwt).token, username = ((dynamic)jwt).username, expires = ((dynamic)jwt).expires, refreshTokenExpires = ((dynamic)jwt).refreshTokenExpires, returnUrl });
            }
        }

        return Ok(jwt);
    }

    [HttpPost("LoginWithGoogle")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> LoginWithGoogle([FromBody] LoginWithSsoModel model)
    {
        var authSettings = configuration.GetSection("AuthSettings").Get<AuthSettings>()!;
        var ssoSettings = authSettings.SsoProviders.Single(x => x.Name == "Google");
        if (!ssoSettings.Enabled) return BadRequest("Sign in with Google is not enabled.");

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new List<string> { _simpleAuthSettings.GoogleClientId }
        };

        GoogleJsonWebSignature.Payload payload;

        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(model.CredentialsFromProvider, settings);
        }
        catch (Exception)
        {
            return BadRequest("Invalid Google credentials.");
        }

        var processSsoLoginResult = await ProcessSsoUserLogin(payload.Email, model.DeviceId);

        return !string.IsNullOrEmpty(processSsoLoginResult.Error)
            ? Unauthorized(GetErrorResponse(processSsoLoginResult.Error))
            : Ok(processSsoLoginResult.Jwt);
    }

    [HttpPost("LoginWithFacebook")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> LoginWithFacebook([FromBody] LoginWithSsoModel model)
    {
        var authSettings = configuration.GetSection("AuthSettings").Get<AuthSettings>()!;
        var ssoSettings = authSettings.SsoProviders.Single(x => x.Name == "Facebook");
        if (!ssoSettings.Enabled) return BadRequest("Sign in with Facebook is not enabled.");

        var tokenResponse = await httpClient.GetAsync($"https://graph.facebook.com/debug_token?input_token={model.CredentialsFromProvider}&access_token={ssoSettings.AppId}|{ssoSettings.AppSecret}");
        var stringResponse = await tokenResponse.Content.ReadAsStringAsync();
        var facebookUser = JsonConvert.DeserializeObject<FacebookUser>(stringResponse);
        if (facebookUser == null) return Unauthorized(GetErrorResponse("User not found."));
        if (!facebookUser.FacebookUserData.IsValid) return Unauthorized(GetErrorResponse("Invalid Facebook credentials"));

        var meResponse = await httpClient.GetAsync($"https://graph.facebook.com/me?fields=first_name,last_name,email,id&access_token={model.CredentialsFromProvider}");
        var userStringResponse = await meResponse.Content.ReadAsStringAsync();
        var facebookUserInfo = JsonConvert.DeserializeObject<FacebookUserInfo>(userStringResponse);

        if (facebookUserInfo == null) return BadRequest(GetErrorResponse("No matching user info was available for the user."));

        var processSsoLoginResult = await ProcessSsoUserLogin(facebookUserInfo.Email, model.DeviceId);

        return !string.IsNullOrEmpty(processSsoLoginResult.Error)
            ? Unauthorized(GetErrorResponse(processSsoLoginResult.Error))
            : Ok(processSsoLoginResult.Jwt);
    }

    [HttpPost("LoginWithMicrosoft")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> LoginWithMicrosoft([FromBody] LoginWithSsoModel model)
    {

        var authSettings = configuration.GetSection("AuthSettings").Get<AuthSettings>()!;
        var ssoSettings = authSettings.SsoProviders.Single(x => x.Name == "Microsoft");
        if (!ssoSettings.Enabled) return BadRequest("Sign in with Microsoft is not enabled.");

        var tokenEndpoint = $"https://login.microsoftonline.com/{ssoSettings.TenantId}/oauth2/v2.0/token";

        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", ssoSettings.AppId),
            new KeyValuePair<string, string>("client_secret", ssoSettings.AppSecret),
            new KeyValuePair<string, string>("code", model.CredentialsFromProvider),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("redirect_uri", ssoSettings.RedirectUri),
        });

        var response = await httpClient.PostAsync(tokenEndpoint, requestContent);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return BadRequest(GetErrorResponse("Failed to authenticate with Microsoft."));

        var tokenResponse = JsonConvert.DeserializeObject<MicrosoftTokenResponse>(responseString);

        // Use the access token to fetch user profile info
        var microsoftUserInfo = await FetchMicrosoftUserInfo(tokenResponse.AccessToken);

        var processSsoLoginResult = await ProcessSsoUserLogin(microsoftUserInfo.UserPrincipalName, model.DeviceId);

        return !string.IsNullOrEmpty(processSsoLoginResult.Error)
            ? Unauthorized(GetErrorResponse(processSsoLoginResult.Error))
            : Ok(processSsoLoginResult.Jwt);
    }

    #endregion

    #region Logout

    [HttpDelete("Logout")]
    public async Task<IActionResult> Logout()
    {
        if (User.Identity is { IsAuthenticated: true })
        {
            await logger.LogAsync(AuthLogEventType.Logout, User.Identity.Name, null);
        }

        SetJwtAccessTokenCookie("");
        SetJwtRefreshTokenCookie("", DateTime.UtcNow);
        await Task.CompletedTask;
        return Ok();
    }

    #endregion

    #region RefreshToken

    [HttpGet("RefreshToken")]
    [EnableRateLimiting("fixed")]
    public async Task<ActionResult<string>> RefreshToken(string deviceId)
    {
        var tokenValue = Request.Cookies["X-Refresh-Token"];
        if (string.IsNullOrEmpty(tokenValue))
        {
            return Unauthorized("No refresh token provided.");
        }

        // Hash the input token to compare with the database
        var hashedInput = HashToken(tokenValue);

        // Find the refresh token in the database
        var refreshToken = await db.AppRefreshTokens
            .Include(x => x.AppUser)
            .Include(x => x.AppUser.AppUserRoles).ThenInclude(x => x.AppRole)
            .FirstOrDefaultAsync(x => x.Token == hashedInput && x.DeviceId == deviceId);

        if (refreshToken == null)
        {
            // Reuse detection: the presented token isn't the current one. If it matches a
            // previously-rotated (consumed) token, this is a replay of a stolen token — revoke the
            // entire token family for that user and reject.
            var consumed = await db.AppRefreshTokens
                .FirstOrDefaultAsync(x => x.PreviousToken == hashedInput && x.DeviceId == deviceId);

            if (consumed != null)
            {
                var compromisedUserId = consumed.AppUserId;
                var familyTokens = db.AppRefreshTokens.Where(rt => rt.AppUserId == compromisedUserId);
                db.AppRefreshTokens.RemoveRange(familyTokens);
                await db.SaveChangesAsync();
                await logger.LogAsync(AuthLogEventType.RefreshTokenReuseDetected, consumed.AppUser?.Username ?? compromisedUserId.ToString(),
                    new { deviceId, Message = "Rotated refresh token replayed; revoked all sessions for user." });
                return Unauthorized("The refresh token is invalid or has expired.");
            }

            return Unauthorized("The refresh token is invalid or has expired.");
        }

        if (refreshToken.Expires < DateTime.UtcNow)
        {
            return Unauthorized("The refresh token is invalid or has expired.");
        }

        // Ensure the associated user exists
        if (refreshToken.AppUser == null)
        {
            return Unauthorized("Invalid refresh token - user not found.");
        }

        // Generate a new access token and refresh token
        var jwt = await JwtGenerator(refreshToken.AppUser, deviceId);

        // Generate a new refresh token
        var newRefreshToken = GenerateRefreshToken();

        // Rotate: remember the just-consumed token (the one the client presented) so a later replay
        // of it is detected as reuse. Use hashedInput because JwtGenerator above may have already
        // re-written this row's Token via its own refresh-token write.
        refreshToken.PreviousToken = hashedInput;
        refreshToken.Token = HashToken(newRefreshToken.Token);
        refreshToken.Created = newRefreshToken.Created;
        refreshToken.Expires = newRefreshToken.Expires;

        await db.SaveChangesAsync();

        // Set the new refresh token in a secure cookie
        SetJwtRefreshTokenCookie(newRefreshToken.Token, newRefreshToken.Expires);
        await logger.LogAsync(AuthLogEventType.TokenRefresh, refreshToken.AppUser.Username, new { deviceId });

        return Ok(jwt);
    }

    #endregion

    #region CheckPasswordComplexity

    [HttpGet("CheckPasswordComplexity")]
    public IActionResult CheckPasswordComplexity([FromQuery] string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return BadRequest(new { error = "INVALID_INPUT", message = "Password cannot be empty." });
        }

        var validator = new PasswordComplexityValidator(_authSettings.PasswordComplexityOptions);
        var result = validator.Validate(password);

        if (result.Succeeded)
        {
            return Ok(new { success = true, message = "Password is valid." });
        }

        return Ok(new { success = false, errors = result.Errors });
    }

    #endregion

    #region ForgotPassword / ResetPassword / VerifyAccount / VerifyMfa / SendNewCode

    [HttpPost("ForgotPassword")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
    {
        if (!_simpleAuthSettings.EnableLocalAccounts) return NotFound("Local accounts are disabled");

        var user = await db.AppUsers
            .Include(x => x.AppUserCredential)
            .FirstOrDefaultAsync(x => x.EmailAddress == model.Email);

        // Anti-enumeration: always return the same generic response whether or not the account
        // exists. Only actually issue + send a reset code when a matching account is found.
        // (Residual timing skew: the existing-account path does extra DB/email work; acceptable here.)
        var message = "If an account exists for that email, a password reset code has been sent.";

        if (user != null)
        {
            var verifyToken = await SetupVerifyToken(user);
            // Best-effort send: a delivery failure here must NOT surface (a 500 on the account-exists
            // path vs a 200 for a missing account would re-open the enumeration oracle this endpoint closes).
            await TrySendVerificationEmailBestEffort(user.EmailAddress, verifyToken, "Reset your password");
            if (configuration["AppConfig:Environment:Name"]!.Contains("Local")) message += $" Development ONLY: {verifyToken}";
        }

        return Ok(new { message });
    }

    [HttpPost("ResetPassword")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
    {
        if (!_simpleAuthSettings.EnableLocalAccounts) return NotFound("Local accounts are disabled");

        var query = db.AppUsers
            .Include(x => x.AppUserCredential)
            .Include(x => x.AppUserPasswordHistories);

        var isAdmin = User.IsInRole("Admin");

        // Look up by username alone so a wrong code still resolves the account and counts against
        // its per-account attempt budget. Admins reset without a verification token.
        var user = await query.FirstOrDefaultAsync(x => x.Username == model.Username);

        if (user == null)
            return BadRequest(new { success = false, errors = new List<string> { "Invalid or expired verification token." } });

        // Non-admin callers must present a valid, unexpired, unused reset code.
        if (!isAdmin)
        {
            if (user.AppUserCredential.VerifyTokenExpires < DateTime.UtcNow || user.AppUserCredential.VerifyTokenUsed)
                return BadRequest(new { success = false, errors = new List<string> { "Invalid or expired verification token." } });

            if (user.AppUserCredential.VerifyToken != model.VerifyToken)
            {
                // Wrong code — count it; the token is invalidated once the budget is exhausted.
                await RegisterFailedCodeAttempt(user);
                return BadRequest(new { success = false, errors = new List<string> { "Invalid or expired verification token." } });
            }
        }

        // Validate the new password
        var validator = new PasswordComplexityValidator(_authSettings.PasswordComplexityOptions);
        var result = validator.Validate(model.NewPassword);
        if (!result.Succeeded) return BadRequest(new { success = false, errors = result.Errors });

        // Check if reusing the current password is allowed
        if (_authSettings.PreventReuseOfPreviousPasswords)
        {
            // Only check current password if a hash exists (user has set a password before).
            // Verify handles mixed schemes: legacy rows verify via HMAC+salt, Argon2id rows via
            // the salt embedded in their PHC string.
            if (user.AppUserCredential.PasswordHash != null)
            {
                if (SimpleAuthPasswordHasher.Verify(model.NewPassword, user.AppUserCredential.PasswordHash, user.AppUserCredential.PasswordSalt).Verified)
                {
                    return BadRequest(new { success = false, errors = new List<string> { "New password cannot be the same as the current password." } });
                }
            }

            // Check password history (each entry may be legacy HMAC or Argon2id — Verify handles both)
            foreach (var history in user.AppUserPasswordHistories)
            {
                if (SimpleAuthPasswordHasher.Verify(model.NewPassword, history.HashedPassword, history.Salt).Verified)
                {
                    return BadRequest(new { success = false, errors = new List<string> { "New password cannot be the same as a previously used password." } });
                }
            }
        }

        // Save current password to history (only if a password was previously set)
        if (user.AppUserCredential.PasswordHash != null && user.AppUserCredential.PasswordSalt != null)
        {
            var passwordHistory = new AppUserPasswordHistory
            {
                AppUserId = user.Id,
                HashedPassword = user.AppUserCredential.PasswordHash,
                Salt = user.AppUserCredential.PasswordSalt,
                DateCreated = DateTime.UtcNow
            };

            db.AppUserPasswordHistories.Add(passwordHistory);
            await db.SaveChangesAsync();
        }

        // Invalidate all existing refresh tokens for this user
        var existingTokens = db.AppRefreshTokens.Where(rt => rt.AppUserId == user.Id);
        db.AppRefreshTokens.RemoveRange(existingTokens);
        await db.SaveChangesAsync();

        // Hash and save the new password with Argon2id
        var (newHash, newSalt) = SimpleAuthPasswordHasher.HashPassword(model.NewPassword);
        user.AppUserCredential.PasswordSalt = newSalt;
        user.AppUserCredential.PasswordHash = newHash;
        
        // Only mark token as used if not an admin (since admins don't need a token)
        if (!isAdmin)
        {
            user.AppUserCredential.VerifyTokenUsed = true;
        }

        user.AppUserCredential.PendingMfaLogin = false;
        user.AppUserCredential.FailedVerificationAttempts = 0;
        user.Verified = true;
		
		// Unlock the account if it was locked
        if (user.Locked)
        {
            user.Locked = false;
            user.AppUserCredential.FailedLoginAttempts = 0;
            user.AppUserCredential.LockoutEndTime = null;
            user.AppUserCredential.LastFailedLoginAttempt = null;
        }

        // Indicate cooldown period:
        user.AppUserCredential.VerificationCooldownExpires = DateTime.UtcNow.AddSeconds(_simpleAuthSettings.ResendCodeDelaySeconds);
        await db.SaveChangesAsync();
        await logger.LogAsync(AuthLogEventType.PasswordReset, user.Username, null);
        return Ok(new { success = true, message = "Password reset successfully." });
    }

    [HttpPost("VerifyAccount")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> VerifyAccount([FromBody] VerifyIdentityModel model)
    {
        var user = await db.AppUsers
            .Include(x => x.AppUserCredential)
            .FirstOrDefaultAsync(x => x.Username == model.Username && x.AppUserCredential.VerifyToken == model.VerifyToken && !x.AppUserCredential.PendingMfaLogin);

        if (user == null || user.AppUserCredential.VerifyTokenExpires < DateTime.UtcNow ||
            user.AppUserCredential.VerifyTokenUsed)
            return BadRequest(new { success = false, errors = new List<string> { "Invalid or expired verification token." } });

        if (user.Verified) return Ok(new { success = true, message = "Account already verified..." });
        user.Verified = true;
        await db.SaveChangesAsync();

        await logger.LogAsync(AuthLogEventType.AccountVerified, user.Username, null);
        return Ok(new { success = true, message = "Account verified successfully..." });
    }

    [HttpPost("VerifyMfa")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyIdentityModel model)
    {
        if (!_simpleAuthSettings.EnableLocalAccounts) return NotFound("Local accounts are disabled");

        // Resolve by username + pending state (not the code) so a wrong code still counts against
        // the account's per-account attempt budget.
        var user = await db.AppUsers
            .Include(x => x.AppUserCredential)
            .FirstOrDefaultAsync(x => x.Username == model.Username && x.AppUserCredential.PendingMfaLogin);

        if (user == null || user.AppUserCredential.VerifyTokenExpires < DateTime.UtcNow ||
            user.AppUserCredential.VerifyTokenUsed)
            return BadRequest(new { success = false, errors = new List<string> { "Invalid or expired verification token." } });

        if (user.AppUserCredential.VerifyToken != model.VerifyToken)
        {
            // Wrong code — count it; the token is invalidated once the budget is exhausted.
            await RegisterFailedCodeAttempt(user);
            return BadRequest(new { success = false, errors = new List<string> { "Invalid or expired verification token." } });
        }

        // Mark the token as used
        user.AppUserCredential.VerifyTokenUsed = true;
        user.AppUserCredential.PendingMfaLogin = false;
        user.AppUserCredential.FailedVerificationAttempts = 0;

        // Generate JWT after successful MFA verification
        Debug.Assert(model.DeviceId != null, "model.DeviceId != null");
        var jwt = await JwtGenerator(user, model.DeviceId);
        await db.SaveChangesAsync();

        var data = new { Type = model is VerifyOtpModel ? "OTP" : "Email/SMS", model.DeviceId };
        await logger.LogAsync(AuthLogEventType.MfaVerified, user.Username, data);
        return Ok(jwt);
    }

    [HttpPost("SendNewCode")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> SendNewCode([FromBody] SendNewCodeModel model)
    {
        if (!_simpleAuthSettings.EnableLocalAccounts) return NotFound("Local accounts are disabled");

        var user = await db.AppUsers
            .Include(x => x.AppUserCredential)
            .FirstOrDefaultAsync(x => x.Username == model.Username);

        if (user == null) return NotFound("User not found.");

        var now = DateTime.UtcNow;

        // Enforce cooldown
        if (user.AppUserCredential.VerificationCooldownExpires.HasValue &&
            user.AppUserCredential.VerificationCooldownExpires > now)
        {
            var remainingTime = (user.AppUserCredential.VerificationCooldownExpires.Value - now).TotalSeconds;
            return BadRequest(new
            {
                success = false,
                message = "You must wait before requesting another verification code.",
                remainingSeconds = (int)Math.Ceiling(remainingTime)
            });
        }

        // Generate a new verification code
        var verifyToken = await SetupVerifyToken(user, true);

        // Send the verification code based on the method
        if (model.MfaMethod == MfaMethod.Email)
        {
            await SendVerificationEmail(user.EmailAddress, verifyToken, "New MFA Verification Code");
        }
        else if (model.MfaMethod == MfaMethod.Sms)
        {
            await SendVerificationSms(user.PhoneNumber, verifyToken);
        }
        else
        {
            return BadRequest("Unsupported MFA method.");
        }

        user.AppUserCredential.VerificationCooldownExpires = now.AddSeconds(_simpleAuthSettings.ResendCodeDelaySeconds);

        await db.SaveChangesAsync();

        var message = "A new verification code has been sent.";
        if (configuration["AppConfig:Environment:Name"]!.Contains("Local")) message += $" Development ONLY: {verifyToken}";

        return Ok(new { success = true, message });
    }

    #endregion

    #region Authenticator / OTP Endpoints

    [HttpPost("SetupAuthenticator")]
    [Authorize]
    public async Task<IActionResult> SetupAuthenticator([FromQuery] string username)
    {
        if (!_simpleAuthSettings.EnableMfaViaOtp) return NotFound("OTP MFA is disabled");

        if (User.Identity == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest("Username must be provided.");
        }

        if (!User.IsInRole("Admin") && User.Identity.Name != username)
        {
            return Forbid("You do not have access to this OTP QR Code.");
        }

        var user = await db.AppUsers
            .Include(x => x.AppUserCredential)
            .FirstOrDefaultAsync(x => x.Username == username);

        if (user == null) return NotFound("User not found.");

        if (string.IsNullOrEmpty(user.AppUserCredential.TotpSecret))
        {
            user.AppUserCredential.TotpSecret = GenerateTotpSecret();
            await db.SaveChangesAsync();
        }

        var issuer = _authSettings.OtpIssuerName;
        var label = $"{issuer}:{user.Username}";
        var qrCodeUrl = $"otpauth://totp/{label}?secret={user.AppUserCredential.TotpSecret}&issuer={issuer}";

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(qrCodeUrl, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);
        var qrCodeBase64 = Convert.ToBase64String(qrCodeImage.ToArray());

        var totpSecret = configuration["AppConfig:Environment:Name"]!.Contains("Local") ? $"Development ONLY: {user.AppUserCredential.TotpSecret}" : "REDACTED";

        return Ok(new
        {
            qrCodeBase64,
            totpSecret
        });
    }

    [HttpPost("VerifyAuthenticatorCode")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> VerifyAuthenticatorCode([FromBody] VerifyOtpModel model)
    {
        if (!_simpleAuthSettings.EnableMfaViaOtp) return NotFound("OTP MFA is disabled");

        if (string.IsNullOrEmpty(model.DeviceId))
            return BadRequest(new { success = false, message = "Device ID is required." });

        var user = await db.AppUsers
            .Include(x => x.AppUserCredential)
            .FirstOrDefaultAsync(x => x.Username == model.Username);

        if (user == null || string.IsNullOrEmpty(user.AppUserCredential.TotpSecret))
            return BadRequest("TOTP setup incomplete.");

        // Gate: only a session that has already passed password authentication (Login sets
        // PendingMfaLogin) may complete OTP. This makes TOTP a genuine second factor and prevents an
        // anonymous caller from brute-forcing or locking arbitrary accounts by username. Because this
        // check precedes any failure counting below, it also blocks the counter as a DoS vector.
        if (!user.AppUserCredential.PendingMfaLogin)
            return Unauthorized("No pending MFA login for this account.");

        // Enforce the same account-state checks the standard login path enforces before issuing a token.
        var statusResult = await CheckAccountStatus(user);
        if (statusResult != null) return statusResult;

        var isValid = VerifyTotpCode(user.AppUserCredential.TotpSecret, model.Code);
        if (!isValid)
        {
            // Per-account lockout on repeated TOTP failures. There is no short-lived code to
            // invalidate (the TOTP secret is long-lived), so lock the account like the password path.
            user.AppUserCredential.FailedVerificationAttempts++;
            if (_authSettings.MaxFailedLoginAttempts > 0 &&
                user.AppUserCredential.FailedVerificationAttempts >= _authSettings.MaxFailedLoginAttempts)
            {
                user.Locked = true;
                user.AppUserCredential.LockoutEndTime = _authSettings.AccountLockoutDurationInMinutes > 0
                    ? DateTime.UtcNow.AddMinutes(_authSettings.AccountLockoutDurationInMinutes)
                    : null;
                user.AppUserCredential.FailedVerificationAttempts = 0;
                await db.SaveChangesAsync();
                return Unauthorized("The account has been locked due to multiple failed verification attempts.");
            }

            await db.SaveChangesAsync();
            return Unauthorized("Invalid TOTP code.");
        }

        // Success — clear the pending state and reset the counter, then issue the token.
        user.AppUserCredential.PendingMfaLogin = false;
        user.AppUserCredential.FailedVerificationAttempts = 0;

        var jwt = await JwtGenerator(user, model.DeviceId);
        await db.SaveChangesAsync();
        await logger.LogAsync(AuthLogEventType.MfaVerified, user.Username, new { Type = "OTP", model.DeviceId });

        return Ok(new
        {
            success = true,
            message = "MFA verification successful.",
            token = jwt.token,
            expires = jwt.expires,
            refreshTokenExpires = jwt.refreshTokenExpires
        });
    }

    #endregion

    #region WhoAmI

    [HttpGet("WhoAmI")]
    public async Task<IActionResult> WhoAmI()
    {
        if (User.Identity == null) return Ok("Nobody");
        if (!User.Identity.IsAuthenticated) return Ok("Not Authenticated");
        await Task.CompletedTask;
        return Ok(User.Identity.Name);
    }

    #endregion

    #region UserVerified

    [HttpGet("UserVerified")]
    [EnableRateLimiting("fixed")]
    public async Task<ActionResult<bool>> UserVerified([FromQuery] string username)
    {
        if (string.IsNullOrEmpty(username)) return BadRequest("Username must be provided.");
        var appUser = await db.AppUsers.SingleOrDefaultAsync(x => x.Username == username);
        // Anti-enumeration: don't disclose whether the account exists. A missing account is reported
        // as "not verified" — identical to an existing-but-unverified one. The client routes both to
        // the verification-pending page; a real login attempt then fails with a generic error.
        return Ok(appUser is { Verified: true });
    }

    #endregion

    #region Private methods

    private async Task<ProcessSsoUserLoginResult> ProcessSsoUserLogin(string username, string deviceId)
    {
        var retVal = new ProcessSsoUserLoginResult { Username = username };

        var user = await GetUserWithCredentialsAndRoles(username);

        // ZOMBIE - May implement this in a future version:
        //if (user == null)
        //{
        //    // Optionally create a new account if not found
        //    user = new AppUser
        //    {
        //        Username = email,
        //        EmailAddress = email,
        //        Active = true,
        //        Verified = true
        //    };
        //    await db.AppUsers.AddAsync(user);
        //    await db.SaveChangesAsync();
        //}

        if (user == null)
        {
            retVal.Error = "No user associated with the provided email.";
            return retVal;
        }

        if (!user.Active)
        {
            retVal.Error = "No user is inactive.";
            return retVal;
        }

        var isLocked = await HandleLockedAccounts(user);
        if (isLocked)
        {
            retVal.Error = "The account is locked.";
            return retVal;
        }

        // Assume email is verified by receiving valid credentials from SSO
        if (!user.Verified) user.Verified = true;
        await db.SaveChangesAsync();

        // Generate JWT
        retVal.Jwt = await JwtGenerator(user, deviceId);
        await logger.LogAsync(AuthLogEventType.LoginSuccess, user.Username, new { deviceId, ssoProvider = "SSO" });
        return retVal;
    }

    private class ProcessSsoUserLoginResult
    {
        public string Username { get; set; }
        public string Error { get; set; }
        public dynamic Jwt { get; set; }
    }

    private dynamic GetErrorResponse(string message)
    {
        // return new { success = false, errors = new List<string> { message } };
        return new { message };
    }

    private async Task<MicrosoftUserInfo> FetchMicrosoftUserInfo(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var userInfoString = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<MicrosoftUserInfo>(userInfoString);
    }

    private async Task<AppUser?> GetUserWithCredentialsAndRoles(string username)
    {
        var user = await db.AppUsers
            .Include(x => x.AppUserCredential)
            .Include(x => x.AppUserRoles)
            .ThenInclude(x => x.AppRole)
            .SingleOrDefaultAsync(x => x.Username == username);

        return user;
    }

    private async Task<bool> HandleLockedAccounts(AppUser user)
    {
        if (!user.Locked) return false;

        if (_authSettings.AccountLockoutDurationInMinutes > 0 &&
            user.AppUserCredential.LockoutEndTime <= DateTime.UtcNow)
        {
            // Unlock account after lockout duration
            user.Locked = false;
            user.AppUserCredential.FailedLoginAttempts = 0;
            user.AppUserCredential.LockoutEndTime = null;
            await db.SaveChangesAsync();
        }
        else
        {
            user.AppUserCredential.LockoutEndTime = _authSettings.AccountLockoutDurationInMinutes > 0
                ? DateTime.UtcNow.AddMinutes(_authSettings.AccountLockoutDurationInMinutes)
                : null;

            await db.SaveChangesAsync();
            return true;
        }

        return false;
    }

    // Shared account-state gate used before issuing a token on any authentication path
    // (password login and the OTP second-factor endpoint). Returns null when the account is
    // usable, otherwise the appropriate error result. Keeps the checks — and their order —
    // identical across entry points so a locked/inactive/unverified account can never obtain a token.
    private async Task<IActionResult?> CheckAccountStatus(AppUser user)
    {
        if (!user.Active) return Unauthorized("The user is inactive.");
        if (await HandleLockedAccounts(user)) return Unauthorized("The account is locked.");
        if (_simpleAuthSettings.RequireUserVerification && !user.Verified) return Unauthorized("The user has not yet been verified.");
        return null;
    }

    // Records a failed attempt against a short-lived verification code (email/SMS MFA or
    // password-reset). Once the per-account budget is exhausted the current code is invalidated,
    // forcing the caller to request a fresh one — this throttles distributed/multi-IP brute force
    // that the per-IP rate limiter alone cannot stop.
    private async Task RegisterFailedCodeAttempt(AppUser user)
    {
        user.AppUserCredential.FailedVerificationAttempts++;
        if (_authSettings.MaxFailedLoginAttempts > 0 &&
            user.AppUserCredential.FailedVerificationAttempts >= _authSettings.MaxFailedLoginAttempts)
        {
            // Invalidate the current code; the user must request a new one via SendNewCode/ForgotPassword.
            user.AppUserCredential.VerifyTokenUsed = true;
            user.AppUserCredential.FailedVerificationAttempts = 0;
        }

        await db.SaveChangesAsync();
    }

    private async Task SendVerificationEmail(string email, string token, string subject)
    {
        // Get the user's name for personalization (if available)
        var user = await db.AppUsers.FirstOrDefaultAsync(x => x.EmailAddress == email);
        var userName = user?.FirstName ?? "User";
        
        // Get the app name from configuration (using OtpIssuerName as the app name)
        var appName = _authSettings.OtpIssuerName ?? "Your Application";
        
        // Build email body based on the subject/context
        string emailBody;
        
        if (subject.Contains("Reset your password"))
        {
            emailBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                    <p>Dear {userName},</p>
                    
                    <p>You have requested to reset your password for the system maintained by {appName}.</p>
                    
                    <p>Please use the following verification code to complete your password reset:</p>
                    
                    <p style='font-size: 20px; font-weight: bold; padding: 10px; background-color: #f0f0f0; display: inline-block;'>{token}</p>
                    
                    <p>If you did not make this request, please disregard this message.</p>
                    
                    <p>Sincerely,<br>
                    {appName}</p>
                </body>
                </html>";
        }
        else if (subject.Contains("MFA Verification") || subject.Contains("New MFA"))
        {
            emailBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                    <p>Dear {userName},</p>
                    
                    <p>You have requested a multi-factor authentication code to access the system maintained by {appName}.</p>
                    
                    <p>Please use the following verification code to complete your login:</p>
                    
                    <p style='font-size: 20px; font-weight: bold; padding: 10px; background-color: #f0f0f0; display: inline-block;'>{token}</p>
                    
                    <p>If you did not make this request, please disregard this message.</p>
                    
                    <p>Sincerely,<br>
                    {appName}</p>
                </body>
                </html>";
        }
        else if (subject.Contains("Verify your email"))
        {
            emailBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                    <p>Dear {userName},</p>
                    
                    <p>You have created a new account for the system maintained by {appName}.</p>
                    
                    <p>Please use the following verification code to verify your email address:</p>
                    
                    <p style='font-size: 20px; font-weight: bold; padding: 10px; background-color: #f0f0f0; display: inline-block;'>{token}</p>
                    
                    <p>If you did not make this request, please disregard this message.</p>
                    
                    <p>Sincerely,<br>
                    {appName}</p>
                </body>
                </html>";
        }
        else
        {
            // Default fallback template
            emailBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                    <p>Dear {userName},</p>
                    
                    <p>You have requested a verification code for the system maintained by {appName}.</p>
                    
                    <p>Please use the following verification code:</p>
                    
                    <p style='font-size: 20px; font-weight: bold; padding: 10px; background-color: #f0f0f0; display: inline-block;'>{token}</p>
                    
                    <p>If you did not make this request, please disregard this message.</p>
                    
                    <p>Sincerely,<br>
                    {appName}</p>
                </body>
                </html>";
        }
        
        var mailMessage = new MailMessage
        {
            Subject = subject,
            Body = emailBody,
            IsBodyHtml = true
        };

        mailMessage.To.Add(email);

        await emailSender.SendAsync(mailMessage);
    }

    // Anti-enumeration helper for ForgotPassword. SendVerificationEmail runs only when the account
    // exists, so an exception (SMTP misconfig, transient delivery failure) would let the global error
    // middleware return 500 for real accounts while missing accounts get a uniform 200 — an account
    // enumeration oracle. Swallow delivery failures on this path so the response stays uniform. Kept in
    // a private helper (not the endpoint body) per the no-try/catch-in-endpoints convention; the other
    // SendVerificationEmail callers (register / MFA) intentionally still let failures bubble.
    private async Task TrySendVerificationEmailBestEffort(string email, string token, string subject)
    {
        try
        {
            await SendVerificationEmail(email, token, subject);
        }
        catch
        {
            // Intentionally ignored — delivery failure must not reveal whether the account exists.
        }
    }

    private async Task SendVerificationSms(string userPhoneNumber, string token)
    {
        var message = $"Your verification code is: {token}";
        await SendSms(userPhoneNumber, message);
    }

    private async Task SendSms(string phoneNumber, string message)
    {
        var smsSettings = configuration.GetSection("SmsSettings").Get<SmsSettings>();

        Debug.Assert(smsSettings != null, nameof(smsSettings) + " != null");
        ISmsProvider smsProvider = new TwilioSmsProvider(smsSettings);

        var smsService = new SmsService(smsProvider, smsSettings.LogDirectory);
        await smsService.SendSmsAsync(phoneNumber, message);

    }

    private async Task<string> SetupVerifyToken(AppUser user, bool mfaToken = false)
    {
        var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[4];
        rng.GetBytes(tokenBytes);
        var verifyToken = (BitConverter.ToUInt32(tokenBytes, 0) % 1_000_000).ToString();

        Debug.Assert(user.AppUserCredential != null, "user.AppUserCredential != null");
        user.AppUserCredential.VerifyToken = verifyToken;
        user.AppUserCredential.VerifyTokenExpires = DateTime.UtcNow.AddMinutes(_authSettings.VerifyTokenExpiresInMinutes);
        user.AppUserCredential.VerifyTokenUsed = false;
        user.AppUserCredential.PendingMfaLogin = mfaToken;
        // A freshly-issued code resets the per-account attempt budget.
        user.AppUserCredential.FailedVerificationAttempts = 0;
        await db.SaveChangesAsync();
        return verifyToken;
    }

    private string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        return Convert.ToBase64String(sha256.ComputeHash(bytes));
    }

    private async Task<dynamic> JwtGenerator(AppUser user, string deviceId)
    {
        user.LastSeen = DateTime.UtcNow;
        // UTF8 to match token validation (Extensions uses Encoding.UTF8); equivalent for ASCII secrets.
        var key = Encoding.UTF8.GetBytes(_authSettings.TokenSecret);
        var expiresInMinutes = _authSettings.AccessTokenExpirationMinutes;
        var refreshTokenExpires = DateTime.UtcNow;

        var tokenHandler = new JwtSecurityTokenHandler();

        var claims = new ClaimsIdentity(new[]
        {
            new Claim("id", user.Username),
            new Claim("sub", user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Username)
        });

        if (_authSettings.Mode == SimpleAuthMode.RelyingApp)
        {
            throw new InvalidOperationException("RelyingApp mode should not issue tokens. Token generation must be handled by the IdentityProvider.");
        }

        if (_authSettings.Mode != SimpleAuthMode.IdentityProvider)
        {
            // Standalone mode: include role claims in JWT (original behavior)
            foreach (var role in user.AppUserRoles)
            {
                claims.AddClaim(new Claim(ClaimTypes.Role, role.AppRole.Name));
            }
        }
        // IdentityProvider mode: no role claims — roles are resolved locally by each relying app

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = claims,
            Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            // Stamp iss/aud when configured so relying apps can validate token provenance/scope.
            Issuer = string.IsNullOrEmpty(_authSettings.TokenIssuer) ? null : _authSettings.TokenIssuer,
            Audience = string.IsNullOrEmpty(_authSettings.TokenAudience) ? null : _authSettings.TokenAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var encryptedToken = tokenHandler.WriteToken(token);
        var expires = tokenDescriptor.Expires.Value;

        SetJwtAccessTokenCookie(encryptedToken);

        if (_authSettings.UseRefreshTokens)
        {
            var refreshToken = GenerateRefreshToken();
            SetJwtRefreshTokenCookie(refreshToken.Token, refreshToken.Expires);
            refreshTokenExpires = refreshToken.Expires;
            await WriteRefreshTokenToDatabase(refreshToken, user, deviceId);
        }

        if (_authSettings.UseRefreshTokens) encryptedToken = "REDACTED";

        return new
        {
            token = encryptedToken,
            username = user.Username,
            expires = expires.ToString("o"),
            refreshTokenExpires = refreshTokenExpires.ToString("o") // Return in ISO 8601 format
        };
    }

    private void SetJwtAccessTokenCookie(string encryptedToken)
    {
        if (!_authSettings.StoreTokensInCookies) return;

        var expireInMinutes = _authSettings.AccessTokenExpirationMinutes;
        // Honor forwarded proto (IsHttps reflects X-Forwarded-Proto once ForwardedHeaders is wired)
        // and allow config to force Secure behind a TLS-terminating proxy.
        var secure = _authSettings.AlwaysUseSecureCookies || HttpContext.Request.IsHttps;
        var cookieOptions = new CookieOptions
        {
            Expires = DateTime.UtcNow.AddMinutes(expireInMinutes),
            HttpOnly = true,
            Secure = secure,
            IsEssential = true,
            SameSite = secure ? SameSiteMode.None : SameSiteMode.Lax
        };

        if (!string.IsNullOrEmpty(_authSettings.CookieDomain))
        {
            cookieOptions.Domain = _authSettings.CookieDomain;
            cookieOptions.Secure = true;
            cookieOptions.SameSite = SameSiteMode.Lax;
        }

        HttpContext.Response.Cookies.Append("X-Access-Token", encryptedToken, cookieOptions);
    }

    private void SetJwtRefreshTokenCookie(string tokenValue, DateTime expires)
    {
        if (!_authSettings.StoreTokensInCookies) return;

        var secure = _authSettings.AlwaysUseSecureCookies || HttpContext.Request.IsHttps;
        var cookieOptions = new CookieOptions
        {
            Expires = expires,
            HttpOnly = true,
            Secure = secure,
            IsEssential = true,
            SameSite = secure ? SameSiteMode.None : SameSiteMode.Lax
        };

        if (!string.IsNullOrEmpty(_authSettings.CookieDomain))
        {
            cookieOptions.Domain = _authSettings.CookieDomain;
            cookieOptions.Secure = true;
            cookieOptions.SameSite = SameSiteMode.Lax;
        }

        HttpContext.Response.Cookies.Append("X-Refresh-Token", tokenValue, cookieOptions);
    }

    private async Task WriteRefreshTokenToDatabase(AppRefreshToken refreshToken, AppUser user, string deviceId)
    {
        var hashedToken = HashToken(refreshToken.Token);

        var existingToken = await db.AppRefreshTokens
            .FirstOrDefaultAsync(x => x.AppUserId == user.Id && x.DeviceId == deviceId);

        if (existingToken != null)
        {
            // Update existing token for this device (fresh login) — clear reuse-detection lineage.
            existingToken.Token = hashedToken;
            existingToken.PreviousToken = null;
            existingToken.Created = refreshToken.Created;
            existingToken.Expires = refreshToken.Expires;
        }
        else
        {
            // Add new token for this device
            var newToken = new AppRefreshToken
            {
                AppUserId = user.Id,
                DeviceId = deviceId, // Store the device ID
                Token = hashedToken,
                Created = refreshToken.Created,
                Expires = refreshToken.Expires
            };
            await db.AppRefreshTokens.AddAsync(newToken);
        }

        await db.SaveChangesAsync();
    }

    private AppRefreshToken GenerateRefreshToken()
    {
        var expiresInDays = _authSettings.RefreshTokenExpirationDays;

        var refreshToken = new AppRefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.UtcNow.AddDays(expiresInDays),
            Created = DateTime.UtcNow
        };

        return refreshToken;
    }

    // Verifies the password against the stored credential. Handles both legacy HMAC-SHA512 rows
    // and Argon2id rows; NeedsRehash signals the caller to upgrade the stored hash on success
    // (rehash-on-login migration). Returns not-verified (rather than throwing) when no hash is set.
    private static VerifyResult CheckPassword(string password, AppUser user)
    {
        Debug.Assert(user.AppUserCredential != null, "user.AppUserCredential != null");
        return SimpleAuthPasswordHasher.Verify(password, user.AppUserCredential.PasswordHash, user.AppUserCredential.PasswordSalt);
    }

    private string GenerateTotpSecret()
    {
        var random = new byte[10];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(random);
        return Base32Encoding.ToString(random); // Use a Base32 encoding library
    }

    private bool VerifyTotpCode(string secret, string code)
    {
        var totp = new OtpNet.Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
    }

    public class AppleKeysResponse
    {
        public List<AppleKey> Keys { get; set; }
    }

    public class AppleKey
    {
        public string Kty { get; set; }
        public string Kid { get; set; }
        public string Use { get; set; }
        public string Alg { get; set; }
        public string N { get; set; }
        public string E { get; set; }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var paddedInput = input.Length % 4 == 0
            ? input
            : input + new string('=', 4 - input.Length % 4);

        return Convert.FromBase64String(paddedInput.Replace('-', '+').Replace('_', '/'));
    }

    private bool IsAllowedReturnUrl(string returnUrl)
    {
        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
            return false;

        // Only permit http/https targets — block javascript:, data:, file:, etc.
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        // Reject embedded credentials (e.g. https://trusted.com@evil.com) — the userinfo is a
        // phishing lure and the real host is whatever follows the '@'.
        if (!string.IsNullOrEmpty(uri.UserInfo))
            return false;

        // Check against CookieDomain (e.g., ".lymestack.com"). Require an exact host match or a
        // proper subdomain (dot boundary) so "evillymestack.com" can't satisfy "lymestack.com".
        if (!string.IsNullOrEmpty(_authSettings.CookieDomain))
        {
            var cookieDomain = _authSettings.CookieDomain.TrimStart('.');
            if (uri.Host.Equals(cookieDomain, StringComparison.OrdinalIgnoreCase) ||
                uri.Host.EndsWith("." + cookieDomain, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Check against AllowedOrigins
        foreach (var origin in _authSettings.AllowedOrigins)
        {
            if (Uri.TryCreate(origin, UriKind.Absolute, out var allowedUri) &&
                uri.Host.Equals(allowedUri.Host, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }


    #endregion

    #region Admin endpoints

    [HttpPost("RevokeAllSessionsForUser")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> RevokeAllSessionsForUser([FromQuery] string username)
    {
        var user = await db.AppUsers.SingleOrDefaultAsync(u => u.Username == username);
        if (user == null) return NotFound("User not found.");

        var tokens = db.AppRefreshTokens.Where(rt => rt.AppUserId == user.Id);
        db.AppRefreshTokens.RemoveRange(tokens);
        await db.SaveChangesAsync();
        await logger.LogAsync(AuthLogEventType.SessionRevoked, username, new { Message = $"Admin {User.Identity.Name} revoked all sessions for {username}" });
        return Ok($"All sessions revoked for user {username}.");
    }

    [HttpPost("RevokeAllSessions")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> RevokeAllSessions()
    {
        var allTokens = db.AppRefreshTokens;
        db.AppRefreshTokens.RemoveRange(allTokens);
        await db.SaveChangesAsync();
        await logger.LogAsync(AuthLogEventType.SessionRevoked, "", new { Message = $"Admin {User.Identity.Name} revoked all sessions for all users." });
        return Ok("All sessions have been revoked.");
    }

    #endregion
}
