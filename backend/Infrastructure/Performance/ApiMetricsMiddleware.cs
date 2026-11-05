namespace Breaku.Infrastructure.Performance;

public sealed class ApiMetricsMiddleware
{
    private readonly RequestDelegate _next;

    public ApiMetricsMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ApiMetrics metrics)
    {
        var measurement = metrics.Start(context.Request.Method, context.Request.Path);
        try
        {
            await _next(context);
        }
        finally
        {
            measurement.Complete(context.Response.StatusCode);
        }
    }
}
