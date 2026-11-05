using Breaku.Application;
using Microsoft.AspNetCore.Mvc;
using Breaku.Infrastructure.Performance;
using System.Globalization;
using System.Text;

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
    public IActionResult Metrics()
    {
        var output = new StringBuilder("# HELP breaku_http_requests_total Total HTTP requests by route.\n");
        output.AppendLine("# TYPE breaku_http_requests_total counter");
        output.AppendLine("# HELP breaku_http_errors_total HTTP 5xx responses by route.");
        output.AppendLine("# TYPE breaku_http_errors_total counter");
        output.AppendLine("# HELP breaku_http_request_duration_ms HTTP request duration statistics.");
        output.AppendLine("# TYPE breaku_http_request_duration_ms gauge");
        foreach (var metric in _metrics.Snapshot())
        {
            var labels = metric.Key.Replace("\"", "\\\"", StringComparison.Ordinal);
            output.AppendLine($"breaku_http_requests_total{{route=\"{labels}\"}} {metric.Value.Requests.ToString(CultureInfo.InvariantCulture)}");
            output.AppendLine($"breaku_http_errors_total{{route=\"{labels}\"}} {metric.Value.Errors.ToString(CultureInfo.InvariantCulture)}");
            output.AppendLine($"breaku_http_request_duration_ms{{route=\"{labels}\",quantile=\"0.5\"}} {metric.Value.P50Milliseconds.ToString(CultureInfo.InvariantCulture)}");
            output.AppendLine($"breaku_http_request_duration_ms{{route=\"{labels}\",quantile=\"0.95\"}} {metric.Value.P95Milliseconds.ToString(CultureInfo.InvariantCulture)}");
        }
        return Content(output.ToString(), "text/plain");
    }
}
