using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// This is a test secure endpoint.
/// </summary>
[Authorize]
[Route("[controller]")]
[ApiController]
public class SecureController : ControllerBase
{
    [HttpGet("GetColorList")]
    public List<string> GetColorList()
    {
        try
        {
            return ["Red", "Green", "Blue"];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}

