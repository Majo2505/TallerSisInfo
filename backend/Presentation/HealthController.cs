using Breaku.Application;
using Microsoft.AspNetCore.Mvc;
using Breaku.Infrastructure.Performance;

namespace Breaku.Presentation;

[ApiController]
[Route("health")]
[Tags("Health")]
public class HealthController : ControllerBase
{
    private readonly IDatabaseHealthCheck _databaseHealthCheck;
    private readonly ApiMetrics _metrics;

    public HealthController(IDatabaseHealthCheck databaseHealthCheck, ApiMetrics metrics)
    {
        _databaseHealthCheck = databaseHealthCheck;
        _metrics = metrics;
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

    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetReady(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));
        var connected = await _databaseHealthCheck.CanConnectAsync(timeout.Token);
        return connected
            ? Ok(new { status = "ready" })
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "not_ready" });
    }

    [HttpGet("metrics")]
    [Produces("text/plain")]
    public IActionResult Metrics() => Content(_metrics.ToPrometheus(), "text/plain");
}
