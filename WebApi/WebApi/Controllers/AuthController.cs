using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SimpleAuthNet;
using SimpleAuthNet.Data;
using SimpleAuthNet.Models;
using SimpleAuthNet.Models.Config;
using SimpleAuthNet.Models.ViewModels;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace WebApi.Controllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class AuthController(IConfiguration configuration, SimpleAuthNetDataContext db) : ControllerBase
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
        }
        else
        {
            return BadRequest("Passwords don't match");
        }

        await db.AppUsers.AddAsync(user);
        await db.SaveChangesAsync();

        // If this is the first user in the system, grant admin access:
        if (userCount != 0) return Ok(user);
        var appUserRole = new AppUserRole
        {
            AppUserId = user.Id,
            AppRoleId = 1
        };

        user.AppUserRoles.Add(appUserRole);
        await db.SaveChangesAsync();
        return Ok(user);
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

    #region Login

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        if (!_appConfig.EnableLocalAccounts) return NotFound("Local Accounts are not enabled.");

        var user = await GetUserWithRoles(model.Username);
        if (user == null) return BadRequest(new { error = "INVALID_CREDENTIALS", message = "AppUser or password was invalid." });
        if (!user.Active) return Unauthorized("The user is inactive.");
        if (_authSettings.RequireUserVerification && !user.Verified) return Unauthorized("The user has not yet been verified.");

        var match = CheckPassword(model.Password, user);
        if (!match) return BadRequest(new { error = "INVALID_CREDENTIALS", message = "AppUser or password was invalid." });

        var jwt = await JwtGenerator(user, model.DeviceId);
        return Ok(jwt);
    }

    [HttpPost("LoginWithGoogle")]
    public async Task<IActionResult> LoginWithGoogle([FromBody] LoginWithGoogleModel model)
    {
        if (!_appConfig.EnableGoogle) return BadRequest("Sign in with Google is not enabled.");

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new List<string> { _appConfig.GoogleClientId }
        };

        var payload = await GoogleJsonWebSignature.ValidateAsync(model.CredentialsFromGoogle, settings);
        var user = await GetUserWithRoles(payload.Email);
        if (user == null) return BadRequest();
        if (!user.Active) return Unauthorized("The user is inactive.");

        // Assume email is verified by receiving valid credentials from Google.
        if (!user.Verified) user.Verified = true;

        var jwt = await JwtGenerator(user, model.DeviceId);
        return Ok(jwt);
    }

    private async Task<AppUser?> GetUserWithRoles(string username)
    {
        var user = await db.AppUsers
            .Include(x => x.AppUserCredential)
            .Include(x => x.AppUserRoles)
            .ThenInclude(x => x.AppRole)
            .SingleOrDefaultAsync(x => x.Username == username);

        return user;
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

    #region ForgotPassword / ResetPassword

    [HttpPost("ForgotPassword")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
    {
        var user = await db.AppUsers
            .Include(x => x.AppUserCredential)
            .FirstOrDefaultAsync(x => x.EmailAddress == model.Email);

        if (user == null)
            return BadRequest("No user found with that email.");

        var resetToken = new Random().Next(100000, 999999).ToString();
        Debug.Assert(user.AppUserCredential != null, "user.AppUserCredential != null");
        user.AppUserCredential.PasswordResetToken = resetToken;
        user.AppUserCredential.PasswordResetExpires = DateTime.UtcNow.AddMinutes(30);
        user.AppUserCredential.PasswordResetUsed = false;
        await db.SaveChangesAsync();
        await SendPasswordResetEmail(user.EmailAddress, resetToken);

        var message = "Password reset email sent.";
        if (_appConfig.Environment.Name.Contains("Local")) message += $" Development ONLY: {resetToken}";
        return Ok(new { message });
    }

    private async Task SendPasswordResetEmail(string email, string token)
    {
        var mailMessage = new MailMessage
        {
            Subject = "Password Reset Verification Code",
            Body = $"Your verification code is: {token}",
            IsBodyHtml = true
        };

        mailMessage.To.Add(email);

        var emailService = new EmailService(configuration);
        emailService.SendEmailMessage(mailMessage);
        await Task.CompletedTask;
    }

    [HttpPost("ResetPassword")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
    {
        // Future: Need to lookup by username in case two tokens exist that are the same. Unlikey, but definitely possible...
        var user = await db.AppUsers
            .Include(x => x.AppUserCredential)
            .FirstOrDefaultAsync(x => x.AppUserCredential.PasswordResetToken == model.Token);

        if (user == null || user.AppUserCredential.PasswordResetExpires < DateTime.UtcNow ||
            user.AppUserCredential.PasswordResetUsed)
            return BadRequest(new { success = false, errors = new List<string> { "Invalid or expired reset token." } });

        // Validate the new password
        var validator = new PasswordComplexityValidator(_authSettings.PasswordComplexityOptions);
        var result = validator.Validate(model.NewPassword);
        if (!result.Succeeded) return BadRequest(new { success = false, errors = result.Errors });

        // Hash and save the new password
        using var hmac = new HMACSHA512();
        user.AppUserCredential.PasswordSalt = hmac.Key;
        user.AppUserCredential.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(model.NewPassword));
        user.AppUserCredential.PasswordResetUsed = true;
        user.Verified = true;

        await db.SaveChangesAsync();
        return Ok(new { success = true, message = "Password reset successfully." });
    }

    #endregion

    #region Private methods

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
        return Ok(appUser is { Verified: true });
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