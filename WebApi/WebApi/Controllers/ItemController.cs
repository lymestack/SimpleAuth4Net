using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// This is a test secure endpoint.
    /// </summary>
    [Authorize]
    [ApiController]
    public class ItemController : ControllerBase
    {

        private readonly List<string> _colorList = ["Red", "Green", "Blue"];

        [HttpGet("GetColorList")]
        public List<string> GetColorList()
        {
            try
            {
                return _colorList;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
