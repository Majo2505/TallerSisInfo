using System.Diagnostics;
using Breaku.Application.Horarios;
using Breaku.Application.Recomendaciones;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Actividades;

/// <summary>
/// Caso de uso de HU11: extracción estructurada por tool calling. Convierte el texto libre del estudiante en los datos de
/// una actividad (qué, a qué hora, cuánto dura y cuántas personas) y dice qué falta preguntar. No crea la actividad:
/// eso lo hace HU10 cuando el estudiante confirma el resumen. Cada extracción queda en el log de auditoría.
/// </summary>
public class ExtraccionActividadService
{
    private readonly IExtractorActividad _extractor;
    private readonly IExtraccionRepository _repo;
    private readonly TimeProvider _reloj;

    public ExtraccionActividadService(IExtractorActividad extractor, IExtraccionRepository repo)
        : this(extractor, repo, TimeProvider.System) { }

    public ExtraccionActividadService(IExtractorActividad extractor, IExtraccionRepository repo, TimeProvider reloj)
    {
        _extractor = extractor;
        _repo = repo;
        _reloj = reloj;
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
            catch (Exception) when (!ct.IsCancellationRequested)
            {
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

    private async Task RechazarAsync(LogAuditoriaRag log, ReglasSeguridadIa.Rechazo rechazo, CancellationToken ct)
    {
        log.Estado = LogAuditoriaRag.EstadoRechazadoSeguridad;
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
