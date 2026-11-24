using Breaku.Application.DTOs;

namespace Breaku.Presentation;

/// <summary>
/// HU37: un error que nadie esperaba (una excepción que ningún controlador capturó) responde 500 con
/// <c>{ "codigo": "ERROR_INESPERADO", "mensaje", "codigoSeguimiento" }</c>. El detalle (mensaje de la excepción, traza) solo
/// va al log, junto con el mismo código de seguimiento, para que se pueda rastrear sin filtrar nada interno.
/// No toca nada más: las respuestas de reglas de negocio (400, 401, 403, 404, 409, 429) no son excepciones sin capturar, y
/// los <c>ERROR_INESPERADO</c> que ya devuelven los controladores de admin se quedan como están.
/// </summary>
public sealed class ErroresInesperadosMiddleware
{
    public const string Codigo = "ERROR_INESPERADO";

    private readonly RequestDelegate _next;
    private readonly ILogger<ErroresInesperadosMiddleware> _logger;

    public ErroresInesperadosMiddleware(RequestDelegate next, ILogger<ErroresInesperadosMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (DebeResponderse(ex, context))
        {
            var seguimiento = context.TraceIdentifier;
            _logger.LogError(ex, "Error inesperado en {Metodo} {Ruta} (seguimiento {Seguimiento})",
                context.Request.Method, context.Request.Path.Value, seguimiento);

            // Sin Clear(): los encabezados de CORS que puso la política antes siguen en la respuesta.
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ErrorInesperadoDto(
                Codigo, $"No pudimos completar la operación. Código de seguimiento: {seguimiento}.", seguimiento));
        }
    }

    /// <summary>
    /// No se responde si el cliente se fue (cancelación), si la solicitud estaba mal formada o era demasiado grande (Kestrel ya
    /// sabe qué estado dar: 400, 413…) o si la respuesta ya empezó a enviarse (no se puede cambiar).
    /// </summary>
    public static bool DebeResponderse(Exception ex, HttpContext context)
        => !context.Response.HasStarted
           && !(ex is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
           && ex is not BadHttpRequestException;
}
