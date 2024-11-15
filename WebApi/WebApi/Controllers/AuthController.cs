using Microsoft.AspNetCore.Mvc;
using SimpleNetAuth;
using SimpleNetAuth.Models.ViewModels;

namespace WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = await authService.GetUserWithRoles(model.Username);
        if (user == null || !authService.ValidatePassword(model.Password, user))
            return BadRequest("Invalid username or password");

        var tokens = await authService.GenerateJwtAndRefreshToken(user);
        return Ok(tokens);
    }

    [HttpPost("LoginWithGoogle")]
    public async Task<IActionResult> LoginWithGoogle([FromBody] string credential)
    {
        var user = await authService.AuthenticateWithGoogle(credential);
        if (user == null) return BadRequest("Invalid Google credentials");

        var tokens = await authService.GenerateJwtAndRefreshToken(user);
        return Ok(tokens);
    }

    [HttpGet("RefreshToken")]
    public async Task<IActionResult> RefreshToken()
    {
        var tokenValue = Request.Cookies["X-Refresh-Token"];
        var refreshToken = await authService.GetRefreshToken(tokenValue);

        if (refreshToken == null || refreshToken.Expires < DateTime.UtcNow)
            return Unauthorized("Refresh token is invalid or expired");

        var tokens = await authService.GenerateJwtAndRefreshToken(refreshToken.AppUser);
        return Ok(tokens);
    }

    [HttpPost("Register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (await authService.GetUserWithRoles(model.Username) != null)
            return BadRequest("User already exists");

        // Registration logic goes here.
        return Ok("Registration successful");
    }
}
