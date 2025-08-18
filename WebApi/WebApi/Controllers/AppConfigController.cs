using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleAuthNet.Models.Config;

namespace WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class AppConfigController(IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public ActionResult<SimpleAuthSettings> Get()
    {
        var authSettings = configuration.GetSection("AuthSettings").Get<AuthSettings>();
        var appConfig = configuration.GetSection("AppConfig").Get<AppConfig>();

        var clientSsoProviders = authSettings.SsoProviders?
            .Where(p => p.Enabled)
            .Select(p => new ClientSsoProviderSettings
            {
                Name = p.Name,
                Enabled = true,
                AppId = p.AppId,
                TenantId = p.TenantId,
                RedirectUri = p.RedirectUri
            })
            .ToList();

        appConfig.SimpleAuth.SsoProviders = clientSsoProviders ?? [];
        appConfig.SimpleAuth.PasswordComplexity = authSettings.PasswordComplexityOptions;
        appConfig.SessionId = Guid.NewGuid();
        return Ok(appConfig);
    }
}