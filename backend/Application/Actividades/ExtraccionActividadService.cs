using System.Diagnostics;
using Breaku.Application.Horarios;
using Breaku.Application.Recomendaciones;
using Breaku.Domain;
using Breaku.Domain.Rules;
using Microsoft.Extensions.Logging;

namespace Breaku.Application.Actividades;

/// <summary>
/// Caso de uso de HU11: extracción estructurada por tool calling. Convierte el texto libre del estudiante en los datos de
/// una actividad (qué, a qué hora, cuánto dura y cuántas personas) y dice qué falta preguntar. No crea la actividad:
/// eso lo hace HU10 cuando el estudiante confirma el resumen. Cada extracción queda en el log de auditoría.
/// Si la IA entendió mal, el estudiante puede corregirlo en palabras (<see cref="ExtraerAsync"/> con lo ya entendido)
/// o editando los datos a mano (<see cref="RevisarAsync"/>).
/// </summary>
public class ExtraccionActividadService
{
    /// <summary>Valor de log_auditoria_rag.modelo cuando los datos los corrigió el estudiante a mano (sin LLM).</summary>
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
            Tipo = LogAuditoriaRag.TipoExtraccionComunidad,
            EntradaUsuario = texto,
            CreatedAt = ahoraUtc
        };

        // Los mismos guardrails que el asistente de recomendaciones (HU17), antes de llamar al modelo.
        // Lo «ya entendido» llega del cliente, así que también se revisa.
        var rechazo = ReglasSeguridadIa.Evaluar(texto) ?? ReglasSeguridadIa.Evaluar(anterior?.Actividad);
        if (rechazo is not null) await RechazarAsync(log, rechazo, ct);

        ResultadoExtraccion resultado;
        if (anterior is null && ReglasExtraccion.EsDemasiadoCorto(texto))
        {
            // Sin ninguna actividad reconocible no se llama al LLM.
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
                // La causa (tiempo de espera, respuesta ilegible, error del proveedor) solo va a la terminal, nunca al estudiante.
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

            // Lo que propone el modelo también pasa por los guardrails: nunca se devuelve una actividad no permitida.
            var rechazoSalida = ReglasSeguridadIa.Evaluar(respuesta.Argumentos.Actividad);
            if (rechazoSalida is not null) await RechazarAsync(log, rechazoSalida, ct);

            resultado = ReglasExtraccion.Interpretar(respuesta.Argumentos, anterior, ahoraLocal);
        }

        log.Estado = resultado.Estado switch
        {
            ReglasExtraccion.EstadoCompleto => LogAuditoriaRag.EstadoOk,
            ReglasExtraccion.EstadoIncompleto => LogAuditoriaRag.EstadoIncompleto,
            _ => LogAuditoriaRag.EstadoSinResultados
        };

        // Se registra ANTES de responder: ninguna extracción se muestra sin quedar en el log.
        await _repo.GuardarLogAsync(log, ct);
        return ADto(log.Id, resultado);
    }

    /// <summary>
    /// Revisa los datos que el estudiante corrigió a mano, con las mismas reglas que la extracción y sin llamar al LLM.
    /// Queda en la auditoría como una extracción más (modelo <see cref="ModeloCorreccionManual"/>), así el <c>consultaId</c>
    /// nuevo es el que se usa para crear la actividad (HU10) y la regla «solo se crea desde una extracción completa» se mantiene.
    /// </summary>
    public async Task<RespuestaExtraccionDto> RevisarAsync(int usuarioId, RevisionActividadDto req, CancellationToken ct = default)
    {
        var texto = LimpiarTexto(req.Texto);
        var datos = LeerAnterior(req.Actividad);
        if (datos?.Actividad is null) throw new ReglaNegocioException("VALIDACION", MensajeFaltaActividad);

        var ahoraUtc = _reloj.GetUtcNow().UtcDateTime;
        var log = new LogAuditoriaRag
        {
            UsuarioId = usuarioId,
            Tipo = LogAuditoriaRag.TipoExtraccionComunidad,
            EntradaUsuario = texto,
            Modelo = ModeloCorreccionManual,
            CreatedAt = ahoraUtc
        };

        // Lo que se escribe a mano pasa por los mismos guardrails (HU17) que lo que propone el modelo.
        var rechazo = ReglasSeguridadIa.Evaluar(texto) ?? ReglasSeguridadIa.Evaluar(datos.Actividad);
        if (rechazo is not null) await RechazarAsync(log, rechazo, ct);

        var resultado = ReglasExtraccion.Interpretar(null, datos, ReglasNotificacion.AHoraLocal(ahoraUtc));
        log.Estado = resultado.Estado == ReglasExtraccion.EstadoCompleto ? LogAuditoriaRag.EstadoOk : LogAuditoriaRag.EstadoIncompleto;

        await _repo.GuardarLogAsync(log, ct);
        return ADto(log.Id, resultado);
    }

    private async Task RechazarAsync(LogAuditoriaRag log, ReglasSeguridadIa.Rechazo rechazo, CancellationToken ct)
    {
        log.Estado = LogAuditoriaRag.EstadoRechazadoSeguridad;
        log.MotivoRechazo = rechazo.Categoria;   // HU17: para que el admin distinga un intento de inyección de otro rechazo
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

    /// <summary>Lo ya entendido llega del cliente: se limpia igual que lo que propone el modelo. null si no trae nada.</summary>
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
