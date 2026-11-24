using System.Text.Json;
using Breaku.Presentation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Breaku.Tests;

/// <summary>HU37: un error inesperado responde 500 ERROR_INESPERADO con código de seguimiento, sin filtrar detalles.</summary>
public sealed class ErroresInesperadosMiddlewareTests
{
    private sealed class LogFalso : ILogger<ErroresInesperadosMiddleware>
    {
        public List<(LogLevel Nivel, Exception? Error, string Mensaje)> Hechos { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Hechos.Add((logLevel, exception, formatter(state, exception)));
    }

    private sealed class RespuestaEmpezada : HttpResponseFeature
    {
        public override bool HasStarted => true;
    }

    private static DefaultHttpContext Contexto(string traza = "traza-123")
    {
        var contexto = new DefaultHttpContext { TraceIdentifier = traza };
        contexto.Request.Method = "PATCH";
        contexto.Request.Path = "/api/admin/eventos/5/bajar";
        contexto.Response.Body = new MemoryStream();
        return contexto;
    }

    private static string Cuerpo(HttpContext contexto)
    {
        contexto.Response.Body.Position = 0;
        return new StreamReader(contexto.Response.Body).ReadToEnd();
    }

    [Fact]
    public async Task Un_error_inesperado_responde_500_con_codigo_y_seguimiento_sin_filtrar_detalles()
    {
        var log = new LogFalso();
        var contexto = Contexto();
        var middleware = new ErroresInesperadosMiddleware(_ => throw new InvalidOperationException("cadena de conexión: Server=db;Password=secreta"), log);

        await middleware.InvokeAsync(contexto);

        Assert.Equal(StatusCodes.Status500InternalServerError, contexto.Response.StatusCode);
        Assert.StartsWith("application/json", contexto.Response.ContentType);
        var cuerpo = Cuerpo(contexto);
        using var json = JsonDocument.Parse(cuerpo);
        var raiz = json.RootElement;
        Assert.Equal("ERROR_INESPERADO", raiz.GetProperty("codigo").GetString());
        Assert.Equal("traza-123", raiz.GetProperty("codigoSeguimiento").GetString());
        Assert.Contains("traza-123", raiz.GetProperty("mensaje").GetString());
        // Nada interno en la respuesta: ni el mensaje de la excepción, ni su tipo, ni la traza.
        Assert.DoesNotContain("secreta", cuerpo);
        Assert.DoesNotContain("InvalidOperationException", cuerpo);
        Assert.DoesNotContain("   at ", cuerpo);
    }

    [Fact]
    public async Task El_error_se_registra_en_el_log_con_su_excepcion_y_el_mismo_codigo_de_seguimiento()
    {
        var log = new LogFalso();
        var error = new InvalidOperationException("detalle interno");
        var middleware = new ErroresInesperadosMiddleware(_ => throw error, log);

        await middleware.InvokeAsync(Contexto("seg-9"));

        var hecho = Assert.Single(log.Hechos);
        Assert.Equal(LogLevel.Error, hecho.Nivel);
        Assert.Same(error, hecho.Error);
        Assert.Contains("seg-9", hecho.Mensaje);
        Assert.Contains("/api/admin/eventos/5/bajar", hecho.Mensaje);
    }

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest)]
    [InlineData(StatusCodes.Status401Unauthorized)]
    [InlineData(StatusCodes.Status403Forbidden)]
    [InlineData(StatusCodes.Status404NotFound)]
    [InlineData(StatusCodes.Status409Conflict)]
    [InlineData(StatusCodes.Status429TooManyRequests)]
    public async Task Las_respuestas_normales_no_se_tocan(int estado)
    {
        var log = new LogFalso();
        var contexto = Contexto();
        var middleware = new ErroresInesperadosMiddleware(async c =>
        {
            c.Response.StatusCode = estado;
            await c.Response.WriteAsync("{\"codigo\":\"YA_REVISADO\"}");
        }, log);

        await middleware.InvokeAsync(contexto);

        Assert.Equal(estado, contexto.Response.StatusCode);
        Assert.Equal("{\"codigo\":\"YA_REVISADO\"}", Cuerpo(contexto));
        Assert.Empty(log.Hechos);
    }

    [Fact]
    public async Task Una_solicitud_mal_formada_o_demasiado_grande_conserva_su_propio_estado()
    {
        var middleware = new ErroresInesperadosMiddleware(
            _ => throw new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge), new LogFalso());

        var error = await Assert.ThrowsAsync<BadHttpRequestException>(() => middleware.InvokeAsync(Contexto()));

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, error.StatusCode);
    }

    [Fact]
    public async Task Si_el_cliente_cancela_la_solicitud_no_se_responde_un_error()
    {
        using var cancelacion = new CancellationTokenSource();
        var contexto = Contexto();
        contexto.RequestAborted = cancelacion.Token;
        await cancelacion.CancelAsync();
        var middleware = new ErroresInesperadosMiddleware(_ => throw new OperationCanceledException(), new LogFalso());

        await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.InvokeAsync(contexto));
    }

    [Fact]
    public async Task Si_la_respuesta_ya_empezo_el_error_sigue_su_camino()
    {
        var contexto = Contexto();
        contexto.Features.Set<IHttpResponseFeature>(new RespuestaEmpezada());
        var middleware = new ErroresInesperadosMiddleware(_ => throw new InvalidOperationException("tarde"), new LogFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(contexto));
    }

    [Fact]
    public async Task Una_excepcion_de_reglas_que_ningun_controlador_capturo_tambien_es_un_error_inesperado()
    {
        var contexto = Contexto();
        var middleware = new ErroresInesperadosMiddleware(
            _ => throw new Breaku.Application.Horarios.ReglaNegocioException("OTRO", "no lo esperaba nadie"), new LogFalso());

        await middleware.InvokeAsync(contexto);

        Assert.Equal(StatusCodes.Status500InternalServerError, contexto.Response.StatusCode);
        Assert.DoesNotContain("no lo esperaba nadie", Cuerpo(contexto));
    }
}
