using System.Diagnostics;
using Breaku.Application.Horarios;
using Breaku.Application.Recomendaciones;
using Breaku.Domain;
using Breaku.Domain.Rules;
using Microsoft.Extensions.Logging;

namespace Breaku.Application.Actividades;

public class ExtraccionActividadService
{
    public const string ModeloCorreccionManual = "manual";

    public const string MensajeFaltaActividad = "Escribe qué actividad vas a hacer.";

    private readonly IExtractorActividad _extractor;
    private readonly IExtraccionRepository _repo;
    private readonly TimeProvider _reloj;
    private readonly ILogger<ExtraccionActividadService>? _logger;

    public ExtraccionActividadService(IExtractorActividad extractor, IExtraccionRepository repo)
        : this(extractor, repo, TimeProvider.System) { }

    public ExtraccionActividadService(IExtractorActividad extractor, IExtraccionRepository repo, ILogger<ExtraccionActividadService> logger)
        : this(extractor, repo, TimeProvider.System, logger) { }

    public ExtraccionActividadService(IExtractorActividad extractor, IExtraccionRepository repo, TimeProvider reloj,
        ILogger<ExtraccionActividadService>? logger = null)
    {
        _extractor = extractor;
        _repo = repo;
        _reloj = reloj;
        _logger = logger;
    }

    public async Task<RespuestaExtraccionDto> ExtraerAsync(int usuarioId, SolicitudExtraccionDto req, CancellationToken ct = default)
    {
        var texto = LimpiarTexto(req.Texto);
        var anterior = LeerAnterior(req.Anterior);
        var ahoraUtc = _reloj.GetUtcNow().UtcDateTime;
        var ahoraLocal = ReglasNotificacion.AHoraLocal(ahoraUtc);

        var log = new LogAuditoriaRag
        {
            UsuarioId = usuarioId,
            ConsultaOrigenId = await OrigenAsync(usuarioId, req.ConsultaAnteriorId, ahoraUtc, ct),
            Tipo = LogAuditoriaRag.TipoExtraccionComunidad,
            EntradaUsuario = texto,
            CreatedAt = ahoraUtc
        };

        var rechazo = ReglasSeguridadIa.Evaluar(texto) ?? ReglasSeguridadIa.Evaluar(anterior?.Actividad);
        if (rechazo is not null) await RechazarAsync(log, rechazo, ct);

        ResultadoExtraccion resultado;
        if (anterior is null && ReglasExtraccion.EsDemasiadoCorto(texto))
        {
            resultado = ReglasExtraccion.Interpretar(null, null, ahoraLocal);
        }
        else
        {
            var prompt = PromptExtraccion.Construir(texto, anterior, ahoraLocal);
            log.PromptEnviado = prompt.ComoTexto();

            var reloj = Stopwatch.StartNew();
            RespuestaExtractor respuesta;
            try
            {
                respuesta = await _extractor.ExtraerAsync(prompt, texto, ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger?.LogWarning(ex, "No se pudo extraer la actividad: {Motivo}", ex.Message);
                log.Estado = LogAuditoriaRag.EstadoError;
                log.LatenciaMs = (int)reloj.ElapsedMilliseconds;
                await _repo.GuardarLogAsync(log, ct);
                throw new ReglaNegocioException("ASISTENTE_NO_DISPONIBLE",
                    "El asistente no está disponible en este momento. Intenta de nuevo en unos minutos.");
            }

            log.Modelo = respuesta.Modelo;
            log.RespuestaLlm = respuesta.TextoCrudo;
            log.LatenciaMs = (int)reloj.ElapsedMilliseconds;

            var rechazoSalida = ReglasSeguridadIa.Evaluar(respuesta.Argumentos.Actividad);
            if (rechazoSalida is not null) await RechazarAsync(log, rechazoSalida, ct);

            resultado = ReglasExtraccion.Interpretar(respuesta.Argumentos, anterior, ahoraLocal);
        }

        log.DatosExtraidos = ResumenSiEstaCompleto(resultado);   // HU10: lo que se va a confirmar
        log.Estado = resultado.Estado switch
        {
            ReglasExtraccion.EstadoCompleto => LogAuditoriaRag.EstadoOk,
            ReglasExtraccion.EstadoIncompleto => LogAuditoriaRag.EstadoIncompleto,
            _ => LogAuditoriaRag.EstadoSinResultados
        };

        await _repo.GuardarLogAsync(log, ct);
        return ADto(log.Id, resultado);
    }

    public async Task<RespuestaExtraccionDto> RevisarAsync(int usuarioId, RevisionActividadDto req, CancellationToken ct = default)
    {
        var texto = LimpiarTexto(req.Texto);
        var datos = LeerAnterior(req.Actividad);
        if (datos?.Actividad is null) throw new ReglaNegocioException("VALIDACION", MensajeFaltaActividad);

        var ahoraUtc = _reloj.GetUtcNow().UtcDateTime;
        var log = new LogAuditoriaRag
        {
            UsuarioId = usuarioId,
            ConsultaOrigenId = await OrigenAsync(usuarioId, req.ConsultaAnteriorId, ahoraUtc, ct),
            Tipo = LogAuditoriaRag.TipoExtraccionComunidad,
            EntradaUsuario = texto,
            Modelo = ModeloCorreccionManual,
            CreatedAt = ahoraUtc
        };

        var rechazo = ReglasSeguridadIa.Evaluar(texto) ?? ReglasSeguridadIa.Evaluar(datos.Actividad);
        if (rechazo is not null) await RechazarAsync(log, rechazo, ct);

        var resultado = ReglasExtraccion.Interpretar(null, datos, ReglasNotificacion.AHoraLocal(ahoraUtc));
        log.Estado = resultado.Estado == ReglasExtraccion.EstadoCompleto ? LogAuditoriaRag.EstadoOk : LogAuditoriaRag.EstadoIncompleto;
        log.DatosExtraidos = ResumenSiEstaCompleto(resultado);   // HU10: lo que se va a confirmar

        await _repo.GuardarLogAsync(log, ct);
        return ADto(log.Id, resultado);
    }

    private async Task<long?> OrigenAsync(int usuarioId, long? consultaAnteriorId, DateTime ahoraUtc, CancellationToken ct)
        => consultaAnteriorId is { } id && id > 0
            ? ReglasExtraccion.OrigenDeConversacion(await _repo.ObtenerLogAsync(id, ct), usuarioId, ahoraUtc)
            : null;

    /// <summary>HU10: solo un resumen completo se puede confirmar; se guarda tal como se le muestra al estudiante.</summary>
    private static string? ResumenSiEstaCompleto(ResultadoExtraccion r)
        => r.Estado == ReglasExtraccion.EstadoCompleto ? ReglasResumenActividad.Serializar(r.Datos) : null;

    private async Task RechazarAsync(LogAuditoriaRag log, ReglasSeguridadIa.Rechazo rechazo, CancellationToken ct)
    {
        log.Estado = LogAuditoriaRag.EstadoRechazadoSeguridad;
        log.MotivoRechazo = rechazo.Categoria;
        await _repo.GuardarLogAsync(log, ct);
        throw new ReglaNegocioException(RecomendacionService.CodigoSolicitudNoPermitida, rechazo.Mensaje);
    }

    private static string LimpiarTexto(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            throw new ReglaNegocioException("VALIDACION", "Escribe qué quieres hacer.");

        var limpio = texto.Trim();
        if (limpio.Length > ReglasExtraccion.MaximoLargoTexto)
            throw new ReglaNegocioException("VALIDACION",
                $"La descripción puede tener como máximo {ReglasExtraccion.MaximoLargoTexto} caracteres.");

        var saneado = ReglasSeguridadIa.Sanear(limpio);
        return saneado.Length == 0
            ? throw new ReglaNegocioException("VALIDACION", "Escribe qué quieres hacer.")
            : saneado;
    }

    private static DatosActividad? LeerAnterior(ActividadExtraidaDto? anterior)
    {
        if (anterior is null) return null;
        var datos = new DatosActividad(
            ReglasExtraccion.LimpiarActividad(anterior.Actividad),
            ReglasExtraccion.LeerHora(anterior.HoraInicio),
            anterior.DuracionMin,
            anterior.MaxParticipantes);
        return datos is { Actividad: null, HoraInicio: null, DuracionMin: null, MaxParticipantes: null } ? null : datos;
    }

    private static RespuestaExtraccionDto ADto(long consultaId, ResultadoExtraccion r) =>
        new(consultaId, r.Estado, r.Mensaje,
            new ActividadExtraidaDto(r.Datos.Actividad, r.Datos.HoraInicio?.ToString("HH:mm"), r.Datos.DuracionMin, r.Datos.MaxParticipantes),
            r.Faltantes.ToList(), r.Supuestos.ToList());
}
