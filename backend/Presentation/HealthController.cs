using Microsoft.AspNetCore.Mvc;

namespace Breaku.Presentation;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();
}
