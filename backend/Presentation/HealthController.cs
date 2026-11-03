using Breaku.Application;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Presentation;

[ApiController]
[Route("health")]
[Tags("Health")]
public class HealthController : ControllerBase
{
    private readonly IDatabaseHealthCheck _databaseHealthCheck;

    public HealthController(IDatabaseHealthCheck databaseHealthCheck)
    {
        _databaseHealthCheck = databaseHealthCheck;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok();

    [HttpGet("db")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDb(CancellationToken cancellationToken)
    {
        var connected = await _databaseHealthCheck.CanConnectAsync(cancellationToken);
        return connected ? Ok() : StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}
