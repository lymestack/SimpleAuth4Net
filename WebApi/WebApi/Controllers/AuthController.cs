using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleNetAuth;
using SimpleNetAuth.Data;
using SimpleNetAuth.Models.ViewModels;

namespace WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(IConfiguration configuration, /* IRecaptchaService recaptchaService, */ SimpleNetAuthDataContext db) : ControllerBase
{
    // TODO: Fix the DI
    private AuthService _authService = new AuthService(configuration, db);

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = await _authService.GetUserWithRoles(model.Username);
        if (user == null || !_authService.ValidatePassword(model.Password, user))
            return BadRequest("Invalid username or password");

        var tokens = await _authService.GenerateJwtAndRefreshToken(user);
        return Ok(tokens);
    }

    [HttpPost("LoginWithGoogle")]
    public async Task<IActionResult> LoginWithGoogle([FromBody] string credential)
    {
        var user = await _authService.AuthenticateWithGoogle(credential);
        if (user == null) return BadRequest("Invalid Google credentials");

        var tokens = await _authService.GenerateJwtAndRefreshToken(user);
        return Ok(tokens);
    }

    [HttpGet("RefreshToken")]
    public async Task<IActionResult> RefreshToken()
    {
        var tokenValue = Request.Cookies["X-Refresh-Token"];
        var refreshToken = await _authService.GetRefreshToken(tokenValue);

        if (refreshToken == null || refreshToken.Expires < DateTime.UtcNow)
            return Unauthorized("Refresh token is invalid or expired");

        var tokens = await _authService.GenerateJwtAndRefreshToken(refreshToken.AppUser);
        return Ok(tokens);
    }

    [HttpPost("Register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model /*, [FromQuery] string captchaToken */)
    {
        if (!ModelState.IsValid) return BadRequest("Invalid input.");

        //var isCaptchaEnabled = configuration.GetValue<bool>("AppConfig:Captcha:Enabled");
        //if (isCaptchaEnabled)
        //{
        //    // Verify the CAPTCHA
        //    var captchaValid = await recaptchaService.ValidateCaptchaAsync(captchaToken);
        //    if (!captchaValid)
        //        return BadRequest("Invalid CAPTCHA.");
        //}

        // HACK: We don't collect email address separately from username during registration:
        model.EmailAddress = model.Username;

        var result = await _authService.RegisterUserAsync(model);
        if (!string.IsNullOrEmpty(result)) return BadRequest(result);

        return Ok("Registration successful");
    }

    [HttpGet("UserExists")]
    [AllowAnonymous]
    public async Task<IActionResult> UserExists([FromQuery] string username)
    {
        if (string.IsNullOrEmpty(username)) return BadRequest("Username must be provided.");
        var exists = await _authService.UserExistsAsync(username);
        return Ok(new { exists });
    }
}
