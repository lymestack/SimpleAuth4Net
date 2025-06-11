using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleAuthNet.Models.Config;
using System.Diagnostics;

namespace WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class AppConfigController(IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public ActionResult<SimpleAuthSettings> Get()
    {
        var appConfig = configuration.GetSection("SimpleAuthSettings").Get<SimpleAuthSettings>();
        Debug.Assert(appConfig != null, nameof(appConfig) + " != null");
        appConfig.SessionId = Guid.NewGuid();
        return Ok(appConfig);
    }
}