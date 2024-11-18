using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SimpleNetAuth.Data;
using SimpleNetAuth.Models;
using SimpleNetAuth.Models.Config;
using SimpleNetAuth.Models.ViewModels;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace WebApi.Controllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class AuthController(IConfiguration configuration, SimpleNetAuthDataContext db) : ControllerBase
{
    private readonly ApiConfig? _apiConfig = configuration.GetSection("ApiConfig").Get<ApiConfig>();
    private readonly AppConfig? _appConfig = configuration.GetSection("AppConfig").Get<AppConfig>();

    #region Register

    [HttpPost("Register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (db.AppUsers.FirstOrDefault(x => x.Username == model.Username) != null) return BadRequest("AppUser already exists...");
        var user = new AppUser { Username = model.Username, AppUserCredential = new AppUserCredential() };

        if (model.ConfirmPassword == model.Password)
        {
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
        return Ok(user);
    }

    #endregion

    #region Login

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = db.AppUsers.Include(x => x.AppUserCredential).FirstOrDefault(x => x.Username == model.Username);
        if (user == null) return BadRequest("AppUser or password was invalid.");

        var match = CheckPassword(model.Password, user);
        if (!match) return BadRequest("AppUser or password was invalid.");

        var jwt = await JwtGenerator(user);
        return Ok(jwt);
    }

    [HttpPost("LoginWithGoogle")]
    public async Task<IActionResult> LoginWithGoogle([FromBody] string credential)
    {
        Debug.Assert(_appConfig != null, nameof(_appConfig) + " != null");

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new List<string> { _appConfig.AuthSettings.GoogleClientId }
        };

        var payload = await GoogleJsonWebSignature.ValidateAsync(credential, settings);
        var user = db.AppUsers.FirstOrDefault(x => x.Username == payload.Email);
        if (user == null) return BadRequest();

        var jwt = await JwtGenerator(user);
        return Ok(jwt);
    }

    #endregion

    #region RefreshToken

    [HttpGet("RefreshToken")]
    public async Task<ActionResult<string>> RefreshToken()
    {
        var tokenValue = Request.Cookies["X-Refresh-Token"];
        var refreshToken = db.AppRefreshTokens.Include(x => x.AppUser).FirstOrDefault(x => x.Token == tokenValue);

        if (refreshToken == null || refreshToken.Expires < DateTime.Now)
        {
            return Unauthorized("The token has expired.");
        }

        Debug.Assert(refreshToken.AppUser != null, "refreshToken.AppUser != null");
        var jwt = await JwtGenerator(refreshToken.AppUser);
        return Ok(jwt);
    }

    #endregion

    #region DELETE / RevokeToken

    [HttpDelete]
    public async Task<IActionResult> RevokeToken(string username)
    {
        //var users = db.AppUsers.Where(x => x.Username == username).ToList();
        //foreach (var user in users) user.Token = "";
        //await db.SaveChangesAsync();
        return Ok();
    }

    #endregion

    #region ZOMBIE: Debug Endpoints

    [HttpGet("WhoAmI")]
    public async Task<IActionResult> WhoAmI()
    {
        if (User.Identity == null) return Ok("Nobody");
        if (!User.Identity.IsAuthenticated) return Ok("Not Authenticated");
        return Ok(User.Identity.Name);
    }

    // ZOMBIE: For Debugging:
    //[HttpGet("CheckTokenCookie")]
    //public IActionResult CheckTokenCookie()
    //{
    //    var token = HttpContext.Request.Cookies["X-Access-Token"];
    //    return Ok(new { token });
    //}

    #endregion

    #region Private methods

    private async Task<dynamic> JwtGenerator(AppUser user)
    {
        Debug.Assert(_apiConfig != null, nameof(_apiConfig) + " != null");
        Debug.Assert(_appConfig != null, nameof(_appConfig) + " != null");

        var authSettings = _appConfig.AuthSettings;
        var key = Encoding.ASCII.GetBytes(_apiConfig.TokenSecret);
        var expiresInDays = authSettings.RefreshTokenExpirationDays;

        var tokenHandler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("id", user.Username),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Username)
            }),
            Expires = DateTime.Now.AddDays(expiresInDays),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var encryptedToken = tokenHandler.WriteToken(token);

        SetJwtAccessTokenCookie(encryptedToken);

        if (authSettings.UseRefreshTokens)
        {
            var refreshToken = GenerateRefreshToken();
            SetJwtRefreshTokenCookie(refreshToken.Token, refreshToken.Expires);
            await WriteRefreshTokenToDatabase(refreshToken, user);
        }

        if (_appConfig.AuthSettings.UseRefreshTokens) encryptedToken = "REDACTED";
        return new { token = encryptedToken, username = user.Username };
    }

    private void SetJwtAccessTokenCookie(string encryptedToken)
    {
        Debug.Assert(_appConfig != null, nameof(_appConfig) + " != null");
        if (!_appConfig.AuthSettings.StoreTokensInCookies) return;

        var expireInMinutes = _appConfig.AuthSettings.AccessTokenExpirationMinutes;

        HttpContext.Response.Cookies.Append("X-Access-Token", encryptedToken,
            new CookieOptions
            {
                Expires = DateTime.Now.AddMinutes(expireInMinutes),
                HttpOnly = true,
                Secure = true,
                IsEssential = true,
                SameSite = SameSiteMode.None
            });
    }

    private void SetJwtRefreshTokenCookie(string tokenValue, DateTime expires)
    {
        Debug.Assert(_appConfig != null, nameof(_appConfig) + " != null");
        if (!_appConfig.AuthSettings.StoreTokensInCookies) return;

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

    private async Task WriteRefreshTokenToDatabase(AppRefreshToken refreshToken, AppUser user)
    {
        var dbItem = db.AppRefreshTokens.FirstOrDefault(x => x.Token == refreshToken.Token) ?? new AppRefreshToken();
        dbItem.AppUserId = user.Id;
        dbItem.Token = refreshToken.Token;
        dbItem.Created = refreshToken.Created;
        dbItem.Expires = refreshToken.Expires;
        await db.SaveChangesAsync();
    }

    private AppRefreshToken GenerateRefreshToken()
    {
        Debug.Assert(_appConfig != null, nameof(_appConfig) + " != null");
        var expiresInDays = _appConfig.AuthSettings.RefreshTokenExpirationDays);

        var refreshToken = new AppRefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.Now.AddDays(expiresInDays),
            Created = DateTime.Now
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
}