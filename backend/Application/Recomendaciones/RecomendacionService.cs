using System.Diagnostics;
using Breaku.Application.Horarios;
using Breaku.Application.Puentes;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Recomendaciones;

/// <summary>
/// Caso de uso de HU7: recomendación con asistente RAG de contexto estricto.
/// Recupera lugares reales del catálogo (HU6), le pide al asistente que los explique y descarta todo lo que
/// no salga de ese contexto. Cada consulta queda en el log de auditoría, con los lugares que la respaldan.
/// </summary>
public class RecomendacionService
{
    public const string EstadoRespuestaOk = "OK";
    public const string EstadoRespuestaSinResultados = "SIN_RESULTADOS";

    private readonly IPuenteRepository _puentes;
    private readonly IRecuperadorLugares _recuperador;
    private readonly IAsistenteIa _asistente;
    private readonly IRecomendacionRepository _repo;

    public RecomendacionService(IPuenteRepository puentes, IRecuperadorLugares recuperador, IAsistenteIa asistente, IRecomendacionRepository repo)
    {
        _puentes = puentes;
        _recuperador = recuperador;
        _asistente = asistente;
        _repo = repo;
    }

    public async Task<RespuestaRecomendacionDto> RecomendarAsync(int usuarioId, SolicitudRecomendacionDto req, CancellationToken ct = default)
    {
        var entrada = LimpiarEntrada(req.Entrada);

        // Solo puentes del propio usuario: uno ajeno responde igual que si no existiera (BOLA).
        var puente = (await _puentes.ListarPorUsuarioAsync(usuarioId, ct)).FirstOrDefault(p => p.Id == req.PuenteId)
                     ?? throw new ReglaNegocioException("NO_ENCONTRADO", "El puente no existe.");
        var minutosDisponibles = (int)(puente.HoraFin - puente.HoraInicio).TotalMinutes;

        var contexto = await _recuperador.RecuperarAsync(usuarioId, puente, ct);
        var candidatos = contexto.Lugares.DistinctBy(l => l.LugarId).ToList();

        var log = new LogAuditoriaRag
        {
            UsuarioId = usuarioId,
            Tipo = LogAuditoriaRag.TipoRecomendacion,
            ZonaId = contexto.ZonaId,
            RadioAplicadoM = contexto.RadioAplicadoM,
            PuenteId = puente.Id,
            TiempoDisponibleMin = (short)minutosDisponibles,
            EntradaUsuario = entrada,
            Estado = LogAuditoriaRag.EstadoSinResultados,
            CreatedAt = DateTime.UtcNow,
            LugaresRecuperados = candidatos.Select((l, i) => new LugarRecuperado
            {
                LugarId = l.LugarId,
                DistanciaM = l.DistanciaM,
                TiempoTrasladoMin = (short)l.TiempoTrasladoMin,
                Orden = (short)(i + 1)
            }).ToList()
        };

        // Sin contexto no se llama al LLM: no hay nada real que recomendar (criterio 4).
        if (candidatos.Count == 0) return await SinResultadosAsync(log, ct);

        var prompt = PromptRecomendacion.Construir(candidatos, minutosDisponibles, entrada);
        log.PromptEnviado = prompt.ComoTexto();

        var reloj = Stopwatch.StartNew();
        RespuestaAsistente respuesta;
        try
        {
            respuesta = await _asistente.GenerarAsync(prompt, ct);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            log.Estado = LogAuditoriaRag.EstadoError;
            log.LatenciaMs = (int)reloj.ElapsedMilliseconds;
            await _repo.GuardarAsync(log, [], ct);
            throw new ReglaNegocioException("ASISTENTE_NO_DISPONIBLE",
                "El asistente no está disponible en este momento. Intenta de nuevo en unos minutos.");
        }

        log.Modelo = respuesta.Modelo;
        log.RespuestaLlm = respuesta.TextoCrudo;
        log.LatenciaMs = (int)reloj.ElapsedMilliseconds;

        // Contexto estricto: se descarta todo lugar que no se haya recuperado y toda explicación con datos externos (criterios 1 y 2).
        var validas = ReglasRecomendacion.FiltrarSoloRecuperados(
            respuesta.Sugerencias.Select(s => (s.LugarId, s.Explicacion)),
            candidatos.ToDictionary(l => l.LugarId, l => l.Nombre));

        if (validas.Count == 0)
        {
            // Si el asistente propuso algo y todo se descartó, queda constancia como rechazo por seguridad.
            if (respuesta.Sugerencias.Count > 0) log.Estado = LogAuditoriaRag.EstadoRechazadoSeguridad;
            return await SinResultadosAsync(log, ct);
        }

        log.Estado = LogAuditoriaRag.EstadoOk;
        var recomendaciones = validas.Select(v => new Recomendacion
        {
            LugarId = v.LugarId,
            Explicacion = v.Explicacion,
            LugarRecuperado = log.LugaresRecuperados.Single(l => l.LugarId == v.LugarId)
        }).ToList();

        // Se registra ANTES de responder: nada se muestra sin quedar en el log (criterio 3).
        await _repo.GuardarAsync(log, recomendaciones, ct);

        var porId = candidatos.ToDictionary(l => l.LugarId);
        return new RespuestaRecomendacionDto(log.Id, EstadoRespuestaOk, null,
            recomendaciones.Select(r => ADto(r, porId[r.LugarId])).ToList());
    }

    private async Task<RespuestaRecomendacionDto> SinResultadosAsync(LogAuditoriaRag log, CancellationToken ct)
    {
        await _repo.GuardarAsync(log, [], ct);
        return new RespuestaRecomendacionDto(log.Id, EstadoRespuestaSinResultados, ReglasRecomendacion.MensajeSinResultados, []);
    }

    private static string? LimpiarEntrada(string? entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada)) return null;
        var limpia = entrada.Trim();
        if (limpia.Length > ReglasRecomendacion.MaximoLargoEntrada)
            throw new ReglaNegocioException("VALIDACION",
                $"La preferencia puede tener como máximo {ReglasRecomendacion.MaximoLargoEntrada} caracteres.");
        return limpia;
    }

    // Las métricas salen del catálogo (candidato), no del texto del asistente.
    private static RecomendacionDto ADto(Recomendacion r, LugarCandidato l) =>
        new(r.Id, l.LugarId, l.Nombre, l.Categoria, l.DistanciaM, l.TiempoTrasladoMin, l.DuracionEstimadaMin, r.Explicacion, r.Feedback);

    public async Task<string> GuardarFeedbackAsync(int usuarioId, long recomendacionId, string? feedback, CancellationToken ct = default)
    {
        var valor = feedback?.Trim().ToUpperInvariant();
        if (valor is not ("LIKE" or "DISLIKE"))
            throw new ReglaNegocioException("VALIDACION", "El feedback debe ser LIKE o DISLIKE.");

        var recomendacion = await _repo.ObtenerDeUsuarioAsync(recomendacionId, usuarioId, ct)
            ?? throw new ReglaNegocioException("NO_ENCONTRADO", "La recomendación no existe.");
        await _repo.GuardarFeedbackAsync(recomendacion, valor, ct);
        return valor;
    }
}
