namespace Breaku.Infrastructure.Performance;

public sealed class ApiMetricsMiddleware
{
    private const string RutaDesconocida = "no_encontrada";
    private readonly RequestDelegate _next;

    public ApiMetricsMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ApiMetrics metrics)
    {
        var inicio = ApiMetrics.Start();
        var status = StatusCodes.Status500InternalServerError;
        try
        {
            await _next(context);
            status = context.Response.StatusCode;
        }
        finally
        {
            var ruta = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? RutaDesconocida;
            metrics.Record(context.Request.Method, ruta, status, inicio);
        }
    }
}
