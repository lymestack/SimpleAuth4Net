using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SimpleNetAuth.Data;
using SimpleNetAuth.Models;
using SimpleNetAuth.Models.ViewModels;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SimpleNetAuth;

public class AuthService(IConfiguration configuration, SimpleNetAuthDataContext db)
{
    public async Task<AppUser?> GetUserWithRoles(string username)
    {
        return await db.AppUsers
            .Include(x => x.AppUserRoles)!
            .ThenInclude(x => x.AppRole)
            .SingleOrDefaultAsync(x => x.Username == username);
    }

    public bool ValidatePassword(string password, AppUser user)
    {
        Debug.Assert(user.AppUserCredential != null, "user.AppUserCredential != null");
        using var hmac = new HMACSHA512(user.AppUserCredential.PasswordSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return computedHash.SequenceEqual(user.AppUserCredential.PasswordHash);
    }

    public async Task<AppUser?> AuthenticateWithGoogle(string credential)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new List<string> { configuration["SimpleNetAuthSettingsGoogleClientId"] }
        };

        var payload = await GoogleJsonWebSignature.ValidateAsync(credential, settings);
        return await db.AppUsers.SingleOrDefaultAsync(x => x.Username == payload.Email);
    }

    public async Task<dynamic> GenerateJwtAndRefreshToken(AppUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(configuration["SimpleNetAuthSettings:Secret"]);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("id", user.Username),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Username)
            }),
            Expires = DateTime.UtcNow.AddMinutes(int.Parse(configuration["SimpleNetAuthSettings:AccessTokenExpirationMinutes"])),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var encryptedToken = tokenHandler.WriteToken(token);

        var refreshToken = GenerateRefreshToken();
        await AddRefreshTokenAsync(refreshToken, user);

        return new { token = encryptedToken, username = user.Username };
    }

    public async Task AddRefreshTokenAsync(AppRefreshToken refreshToken, AppUser user)
    {
        refreshToken.AppUserId = user.Id;
        await db.AppRefreshTokens.AddAsync(refreshToken);
        await db.SaveChangesAsync();
    }

    public async Task<AppRefreshToken?> GetRefreshToken(string tokenValue)
    {
        return await db.AppRefreshTokens.Include(x => x.AppUser)
            .SingleOrDefaultAsync(x => x.Token == tokenValue);
    }

    public AppRefreshToken GenerateRefreshToken()
    {
        return new AppRefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(int.Parse(configuration["SimpleNetAuthSettings:RefreshTokenLengthBytes"]))),
            Expires = DateTime.UtcNow.AddDays(int.Parse(configuration["SimpleNetAuthSettings:RefreshTokenExpirationDays"])),
            Created = DateTime.UtcNow
        };
    }

    public async Task<string?> RegisterUser(RegisterModel model)
    {
        // FUTURE: Check password complexity.

        if (db.AppUsers.FirstOrDefault(x => x.Username == model.Username) != null) return "User already exists.";
        var user = new AppUser { Username = model.Username, FirstName = model.Firstname, LastName = model.LastName, EmailAddress = model.EmailAddress, AppUserCredential = new AppUserCredential() };

        if (model.ConfirmPassword == model.Password)
        {
            using var hmac = new HMACSHA512();
            user.AppUserCredential.PasswordSalt = hmac.Key;
            user.AppUserCredential.PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(model.Password));
        }
        else
        {
            return "Passwords don't match";
        }

        await db.AppUsers.AddAsync(user);
        await db.SaveChangesAsync();
        return null;
    }
}
