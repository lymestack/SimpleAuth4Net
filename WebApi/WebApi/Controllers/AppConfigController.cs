using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleNetAuth.Models.Config;
using System.Diagnostics;

namespace WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class AppConfigController(IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<AppConfig>> Get()
    {
        var appConfig = configuration.GetSection("AppConfig").Get<AppConfig>();
        Debug.Assert(appConfig != null, nameof(appConfig) + " != null");
        appConfig.SessionId = Guid.NewGuid();
        return Ok(appConfig);
    }
}