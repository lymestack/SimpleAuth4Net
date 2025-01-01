using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using OtpNet;
using QRCoder;
using SimpleAuthNet;
using SimpleAuthNet.Data;
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
public class AuthController(IConfiguration configuration, SimpleAuthContext db, HttpClient httpClient) : ControllerBase
{
    private readonly AuthSettings _authSettings = configuration.GetSection("AuthSettings").Get<AuthSettings>()!;
    private readonly AppConfig _appConfig = configuration.GetSection("AppConfig").Get<AppConfig>()!;

    #region Register

    [HttpPost("Register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (!_appConfig.EnableLocalAccounts) return BadRequest("Local Accounts are not enabled.");
        if (!_appConfig.AllowRegistration) return BadRequest("Public registration is not allowed.");
        if (db.AppUsers.FirstOrDefault(x => x.Username == model.Username) != null) return BadRequest("User already exists...");

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

            using var hmac = new HMACSHA512();
            user.AppUserCredential.PasswordSalt = hmac.Key;
            user.AppUserCredential.PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(model.Password));
            user.AppUserCredential.DateCreated = DateTime.UtcNow;
            user.AppUserCredential.VerifyTokenExpires = DateTime.UtcNow;
        }
        else
        {
            return BadRequest("Passwords don't match");
        }

        await db.AppUsers.AddAsync(user);
        await db.SaveChangesAsync();

        var message = "User Registered Successfully";

        if (_appConfig.RequireUserVerification)
        {
            var verifyToken = await SetupVerifyToken(user);
            await SendVerificationEmail(user.EmailAddress, verifyToken, "Verify your email address");
            if (_appConfig.Environment.Name.Contains("Local")) message += $" Development ONLY: {verifyToken}";
        }

        // If this is the first user in the system, grant admin access:
        if (userCount != 0) return Ok(new { success = true, message });
        var appUserRole = new AppUserRole
        {
            AppUserId = user.Id,
            AppRoleId = 1
        };

        user.AppUserRoles.Add(appUserRole);
        await db.SaveChangesAsync();
        return Ok(new { success = true, message });
    }

    #endregion

    #region UserExists

    [HttpGet("UserExists")]
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
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        if (!_appConfig.EnableLocalAccounts) return NotFound("Local Accounts are not enabled.");

        var user = await GetUserWithCredentialsAndRoles(model.Username);
        if (user == null) return BadRequest(new { error = "INVALID_CREDENTIALS", message = "AppUser or password was invalid." });
        if (!user.Active) return Unauthorized("The user is inactive.");

        var isLocked = await HandleLockedAccounts(user);
        if (isLocked) return Unauthorized("The account is locked.");

        if (_appConfig.RequireUserVerification && !user.Verified) return Unauthorized("The user has not yet been verified.");

        var match = CheckPassword(model.Password, user);

        if (!match)
        {
            user.AppUserCredential.FailedLoginAttempts++;
            user.AppUserCredential.LastFailedLoginAttempt = DateTime.UtcNow;

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

        // Reset failed login attempts on successful login
        user.AppUserCredential.FailedLoginAttempts = 0;
        user.AppUserCredential.LastFailedLoginAttempt = null;
        await db.SaveChangesAsync();

        if (_appConfig.EnableMfaViaEmail || _appConfig.EnableMfaViaSms)
        {
            var verifyToken = await SetupVerifyToken(user, true);
            var message = "A verification code has been sent to your email";
            if (_appConfig.Environment.Name.Contains("Local")) message += $" Development ONLY: {verifyToken}";

            if (model.MfaMethod == MfaMethod.Email) await SendVerificationEmail(user.EmailAddress, verifyToken, "MFA Verification Code");
            if (model.MfaMethod == MfaMethod.Sms) await SendVerificationSms(user.PhoneNumber, verifyToken);

            user.AppUserCredential.VerificationCooldownExpires = DateTime.UtcNow.AddSeconds(_appConfig.ResendCodeDelaySeconds);
            await db.SaveChangesAsync();

            return Ok(new
            {
                mfaRequired = true,
                redirectUrl = "/account/verify-account",
                message
            });
        }

        var jwt = await JwtGenerator(user, model.DeviceId);
        return Ok(jwt);
    }

    [HttpPost("LoginWithGoogle")]
    public async Task<IActionResult> LoginWithGoogle([FromBody] LoginWithGoogleModel model)
    {
        if (!_appConfig.EnableGoogleSso) return BadRequest("Sign in with Google is not enabled.");

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new List<string> { _appConfig.GoogleClientId }
        };

        GoogleJsonWebSignature.Payload payload;

        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(model.CredentialsFromGoogle, settings);
        }
        catch (Exception)
        {
            return BadRequest("Invalid Google credentials.");
        }

        var user = await GetUserWithCredentialsAndRoles(payload.Email);
        if (user == null) return BadRequest("No user associated with the provided email.");
        if (!user.Active) return Unauthorized("The user is inactive.");

        var isLocked = await HandleLockedAccounts(user);
        if (isLocked) return Unauthorized("The account is locked.");

        // Assume email is verified by receiving valid credentials from Google
        if (!user.Verified) user.Verified = true;

        // Generate JWT
        var jwt = await JwtGenerator(user, model.DeviceId);
        return Ok(jwt);
    }

    [HttpPost("LoginWithFacebook")]
    public async Task<IActionResult> LoginWithFacebook([FromBody] LoginWithFacebookModel model)
    {
        if (!_appConfig.EnableFacebookSso) return BadRequest("Sign in with Facebook is not enabled.");

        var tokenResponse = await httpClient.GetAsync($"https://graph.facebook.com/debug_token?input_token={model.CredentialsFromFacebook}&access_token={_appConfig.FacebookAppId}|{_authSettings.FacebookAppSecret}");
        var stringResponse = await tokenResponse.Content.ReadAsStringAsync();
        var facebookUser = JsonConvert.DeserializeObject<FacebookUser>(stringResponse);
        if (facebookUser == null) return Unauthorized(GetErrorResponse("User not found."));
        if (!facebookUser.FacebookUserData.IsValid) return Unauthorized(GetErrorResponse("Invalid Facebook credentials"));

        var meResponse = await httpClient.GetAsync($"https://graph.facebook.com/me?fields=first_name,last_name,email,id&access_token={model.CredentialsFromFacebook}");
        var userStringResponse = await meResponse.Content.ReadAsStringAsync();
        var facebookUserInfo = JsonConvert.DeserializeObject<FacebookUserInfo>(userStringResponse);

        if (facebookUserInfo == null) return BadRequest(GetErrorResponse("No matching user info was available for the user."));

        var user = await GetUserWithCredentialsAndRoles(facebookUserInfo.Email);
        if (user == null) return BadRequest(GetErrorResponse("No user associated with the provided email."));
        if (!user.Active) return Unauthorized(GetErrorResponse("The user is inactive."));

        var isLocked = await HandleLockedAccounts(user);
        if (isLocked) return Unauthorized(GetErrorResponse("The account is locked."));

        // Assume email is verified by receiving valid credentials from Facebook
        if (!user.Verified) user.Verified = true;

        // Generate JWT
        var jwt = await JwtGenerator(user, model.DeviceId);
        return Ok(jwt);
    }

    private dynamic GetErrorResponse(string message)
    {
        // return new { success = false, errors = new List<string> { message } };
        return new { message };
    }

    [HttpPost("LoginWithMicrosoft")]
    public async Task<IActionResult> LoginWithMicrosoft([FromBody] LoginWithMicrosoftModel model)
    {
        if (!_appConfig.EnableMicrosoftSso) return BadRequest(GetErrorResponse("Sign in with Microsoft is not enabled."));
        var tokenEndpoint = $"https://login.microsoftonline.com/{_appConfig.MicrosoftTenantId}/oauth2/v2.0/token";

        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _appConfig.MicrosoftClientId),
            new KeyValuePair<string, string>("client_secret", _authSettings.MicrosoftClientSecret),
            new KeyValuePair<string, string>("code", model.AuthorizationCode),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("redirect_uri", _appConfig.MicrosoftRedirectUri),
        });

        var response = await httpClient.PostAsync(tokenEndpoint, requestContent);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return BadRequest(GetErrorResponse("Failed to authenticate with Microsoft."));

        var tokenResponse = JsonConvert.DeserializeObject<MicrosoftTokenResponse>(responseString);

        // Use the access token to fetch user profile info
        var userInfo = await FetchMicrosoftUserInfo(tokenResponse.AccessToken);
        var user = await GetUserWithCredentialsAndRoles(userInfo.UserPrincipalName);
        if (user == null) return BadRequest(GetErrorResponse("No user associated with the provided email."));

        // Generate JWT
        var jwt = await JwtGenerator(user, model.DeviceId);
        return Ok(jwt);
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

    #endregion

    #region Logout

    [HttpDelete("Logout")]
    public async Task<IActionResult> Logout()
    {
        SetJwtAccessTokenCookie("");
        SetJwtRefreshTokenCookie("", DateTime.UtcNow);
        await Task.CompletedTask;
        return Ok();
    }

    #endregion

    #region RefreshToken

    [HttpGet("RefreshToken")]
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

        if (refreshToken == null || refreshToken.Expires < DateTime.UtcNow)
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

        // Update the database atomically
        refreshToken.Token = HashToken(newRefreshToken.Token);
        refreshToken.Created = newRefreshToken.Created;
        refreshToken.Expires = newRefreshToken.Expires;

        await db.SaveChangesAsync();

        // Set the new refresh token in a secure cookie
        SetJwtRefreshTokenCookie(newRefreshToken.Token, newRefreshToken.Expires);

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
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
    {
        var user = await db.AppUsers
            .Include(x => x.AppUserCredential)
            .FirstOrDefaultAsync(x => x.EmailAddress == model.Email);

        if (user == null) return BadRequest("No user found with that email.");

        var verifyToken = await SetupVerifyToken(user);
        await SendVerificationEmail(user.EmailAddress, verifyToken, "Reset your password");

        var message = "Password reset email sent.";
        if (_appConfig.Environment.Name.Contains("Local")) message += $" Development ONLY: {verifyToken}";
        return Ok(new { message });
    }

    [HttpPost("ResetPassword")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
    {
        var user = await db.AppUsers
            .Include(x => x.AppUserCredential)
            .Include(x => x.AppUserPasswordHistories)
            .FirstOrDefaultAsync(x => x.Username == model.Username && x.AppUserCredential.VerifyToken == model.VerifyToken);

        if (user == null || user.AppUserCredential.VerifyTokenExpires < DateTime.UtcNow ||
            user.AppUserCredential.VerifyTokenUsed)
            return BadRequest(new { success = false, errors = new List<string> { "Invalid or expired verification token." } });

        // Validate the new password
        var validator = new PasswordComplexityValidator(_authSettings.PasswordComplexityOptions);
        var result = validator.Validate(model.NewPassword);
        if (!result.Succeeded) return BadRequest(new { success = false, errors = result.Errors });

        // Check if reusing the current password is allowed
        if (_authSettings.PreventReuseOfPreviousPasswords)
        {
            using (var hmac = new HMACSHA512(user.AppUserCredential.PasswordSalt))
            {
                var currentPasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(model.NewPassword));
                if (currentPasswordHash.SequenceEqual(user.AppUserCredential.PasswordHash))
                {
                    return BadRequest(new { success = false, errors = new List<string> { "New password cannot be the same as the current password." } });
                }
            }

            // Check password history
            foreach (var history in user.AppUserPasswordHistories)
            {
                using var hmac = new HMACSHA512(history.Salt); // Use historical salt
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(model.NewPassword));
                if (computedHash.SequenceEqual(history.HashedPassword))
                {
                    return BadRequest(new { success = false, errors = new List<string> { "New password cannot be the same as a previously used password." } });
                }
            }
        }

        // Save current password to history
        var passwordHistory = new AppUserPasswordHistory
        {
            AppUserId = user.Id,
            HashedPassword = user.AppUserCredential.PasswordHash,
            Salt = user.AppUserCredential.PasswordSalt,
            DateCreated = DateTime.UtcNow
        };

        db.AppUserPasswordHistories.Add(passwordHistory);
        await db.SaveChangesAsync();

        // Hash and save the new password
        using var newHmac = new HMACSHA512();
        user.AppUserCredential.PasswordSalt = newHmac.Key;
        user.AppUserCredential.PasswordHash = newHmac.ComputeHash(Encoding.UTF8.GetBytes(model.NewPassword));
        user.AppUserCredential.VerifyTokenUsed = true;
        user.AppUserCredential.PendingMfaLogin = false;
        user.Verified = true;

        // Indicate cooldown period:
        user.AppUserCredential.VerificationCooldownExpires = DateTime.UtcNow.AddSeconds(_appConfig.ResendCodeDelaySeconds);
        await db.SaveChangesAsync();

        return Ok(new { success = true, message = "Password reset successfully." });
    }

    [HttpPost("VerifyAccount")]
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
        return Ok(new { success = true, message = "Account verified successfully..." });
    }

    [HttpPost("VerifyMfa")]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyIdentityModel model)
    {
        var user = await db.AppUsers
            .Include(x => x.AppUserCredential)
            .FirstOrDefaultAsync(x => x.Username == model.Username && x.AppUserCredential.VerifyToken == model.VerifyToken && x.AppUserCredential.PendingMfaLogin);

        if (user == null || user.AppUserCredential.VerifyTokenExpires < DateTime.UtcNow ||
            user.AppUserCredential.VerifyTokenUsed)
            return BadRequest(new { success = false, errors = new List<string> { "Invalid or expired verification token." } });

        // Mark the token as used
        user.AppUserCredential.VerifyTokenUsed = true;
        user.AppUserCredential.PendingMfaLogin = false;

        // Generate JWT after successful MFA verification
        Debug.Assert(model.DeviceId != null, "model.DeviceId != null");
        var jwt = await JwtGenerator(user, model.DeviceId);
        await db.SaveChangesAsync();

        return Ok(jwt);
    }

    [HttpPost("SendNewCode")]
    public async Task<IActionResult> SendNewCode([FromBody] SendNewCodeModel model)
    {
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

        user.AppUserCredential.VerificationCooldownExpires = now.AddSeconds(_appConfig.ResendCodeDelaySeconds);

        await db.SaveChangesAsync();

        var message = "A new verification code has been sent.";
        if (_appConfig.Environment.Name.Contains("Local")) message += $" Development ONLY: {verifyToken}";

        return Ok(new { success = true, message });
    }

    #endregion

    #region Authenticator / OTP Endpoints

    [HttpPost("SetupAuthenticator")]
    public async Task<IActionResult> SetupAuthenticator([FromQuery] string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest("Username must be provided.");
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

        var totpSecret = _appConfig.Environment.Name.Contains("Local") ? $"Development ONLY: {user.AppUserCredential.TotpSecret}" : "REDACTED";

        return Ok(new
        {
            qrCodeBase64,
            totpSecret
        });
    }

    [HttpPost("VerifyAuthenticatorCode")]
    public async Task<IActionResult> VerifyAuthenticatorCode([FromBody] VerifyTotpModel model)
    {
        var user = await db.AppUsers
            .Include(x => x.AppUserCredential)
            .FirstOrDefaultAsync(x => x.Username == model.Username);

        if (user == null || string.IsNullOrEmpty(user.AppUserCredential.TotpSecret))
            return BadRequest("TOTP setup incomplete.");

        var isValid = VerifyTotpCode(user.AppUserCredential.TotpSecret, model.Code);
        if (!isValid) return Unauthorized("Invalid TOTP code.");

        if (string.IsNullOrEmpty(model.DeviceId))
            return BadRequest(new { success = false, message = "Device ID is required." });

        var jwt = await JwtGenerator(user, model.DeviceId);

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
    public async Task<ActionResult<bool>> UserVerified([FromQuery] string username)
    {
        if (string.IsNullOrEmpty(username)) return BadRequest("Username must be provided.");
        var appUser = await db.AppUsers.SingleOrDefaultAsync(x => x.Username == username);
        if (appUser == null) return BadRequest("User didn't exist.");
        return Ok(appUser is { Verified: true });
    }

    #endregion

    #region Private methods

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

    private async Task SendVerificationEmail(string email, string token, string subject)
    {
        var mailMessage = new MailMessage
        {
            Subject = subject,
            Body = $"Your verification code is: {token}",
            IsBodyHtml = true
        };

        mailMessage.To.Add(email);

        var emailService = new EmailService(configuration);
        emailService.SendEmailMessage(mailMessage);
        await Task.CompletedTask;
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
        var verifyToken = new Random().Next(100000, 999999).ToString();
        Debug.Assert(user.AppUserCredential != null, "user.AppUserCredential != null");
        user.AppUserCredential.VerifyToken = verifyToken;
        user.AppUserCredential.VerifyTokenExpires = DateTime.UtcNow.AddMinutes(_authSettings.VerifyTokenExpiresInMinutes);
        user.AppUserCredential.VerifyTokenUsed = false;
        user.AppUserCredential.PendingMfaLogin = mfaToken;
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
        var key = Encoding.ASCII.GetBytes(_authSettings.TokenSecret);
        var expiresInMinutes = _authSettings.AccessTokenExpirationMinutes;
        var refreshTokenExpires = DateTime.UtcNow;

        var tokenHandler = new JwtSecurityTokenHandler();

        var claims = new ClaimsIdentity(new[]
        {
            new Claim("id", user.Username),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Username)
        });

        foreach (var role in user.AppUserRoles)
        {
            claims.AddClaim(new Claim(ClaimTypes.Role, role.AppRole.Name));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = claims,
            Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes),
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

        HttpContext.Response.Cookies.Append("X-Access-Token", encryptedToken,
            new CookieOptions
            {
                Expires = DateTime.UtcNow.AddMinutes(expireInMinutes),
                HttpOnly = true,
                Secure = true,
                IsEssential = true,
                SameSite = SameSiteMode.None
            });
    }

    private void SetJwtRefreshTokenCookie(string tokenValue, DateTime expires)
    {
        if (!_authSettings.StoreTokensInCookies) return;

        HttpContext.Response.Cookies.Append("X-Refresh-Token", tokenValue,
            new CookieOptions
            {
                Expires = expires,
                HttpOnly = true,
                Secure = true,
                IsEssential = true,
                SameSite = SameSiteMode.None
            });
    }

    private async Task WriteRefreshTokenToDatabase(AppRefreshToken refreshToken, AppUser user, string deviceId)
    {
        var hashedToken = HashToken(refreshToken.Token);

        var existingToken = await db.AppRefreshTokens
            .FirstOrDefaultAsync(x => x.AppUserId == user.Id && x.DeviceId == deviceId);

        if (existingToken != null)
        {
            // Update existing token for this device
            existingToken.Token = hashedToken;
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

    private static bool CheckPassword(string password, AppUser user)
    {
        Debug.Assert(user.AppUserCredential != null, "user.AppUserCredential != null");
        using var hmac = new HMACSHA512(user.AppUserCredential.PasswordSalt);
        var compute = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        var result = compute.SequenceEqual(user.AppUserCredential.PasswordHash);
        return result;
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

    #endregion

    #region ZOMBIE - DELETE / RevokeToken - FUTURE: Reimplement as an Admin endpoints

    //[HttpGet("ActiveSessions")]
    //public async Task<IActionResult> ActiveSessions(int userId)
    //{
    //    var sessions = await db.AppRefreshTokens
    //        .Where(x => x.AppUserId == userId)
    //        .Select(x => new { x.DeviceId, x.Created, x.Expires })
    //        .ToListAsync();

    //    return Ok(sessions);
    //}

    //[HttpDelete("RevokeSession")]
    //public async Task<IActionResult> RevokeSession(int userId, string deviceId)
    //{
    //    var session = await db.AppRefreshTokens
    //        .FirstOrDefaultAsync(x => x.AppUserId == userId && x.DeviceId == deviceId);

    //    if (session == null)
    //    {
    //        return NotFound("Session not found.");
    //    }

    //    db.AppRefreshTokens.Remove(session);
    //    await db.SaveChangesAsync();

    //    return Ok("Session revoked.");
    //}

    #endregion
}
