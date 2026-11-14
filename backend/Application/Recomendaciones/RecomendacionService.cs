using System.Diagnostics;
using Breaku.Application.Horarios;
using Breaku.Application.Puentes;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Recomendaciones;

public class RecomendacionService
{
    public const string EstadoRespuestaOk = "OK";
    public const string EstadoRespuestaSinResultados = "SIN_RESULTADOS";
    public const string CodigoSolicitudNoPermitida = "SOLICITUD_NO_PERMITIDA";

    private readonly IPuenteRepository _puentes;
    private readonly IRecuperadorLugares _recuperador;
    private readonly IAsistenteIa _asistente;
    private readonly IRecomendacionRepository _repo;
    private readonly TimeSpan _presupuestoIa;

    public RecomendacionService(IPuenteRepository puentes, IRecuperadorLugares recuperador, IAsistenteIa asistente, IRecomendacionRepository repo)
        : this(puentes, recuperador, asistente, repo, TimeSpan.FromMilliseconds(ReglasRendimiento.PresupuestoIaMs)) { }

    public RecomendacionService(IPuenteRepository puentes, IRecuperadorLugares recuperador, IAsistenteIa asistente, IRecomendacionRepository repo,
        TimeSpan presupuestoIa)
    {
        _puentes = puentes;
        _recuperador = recuperador;
        _asistente = asistente;
        _repo = repo;
        _presupuestoIa = presupuestoIa;
    }

    public async Task<RespuestaRecomendacionDto> RecomendarAsync(int usuarioId, SolicitudRecomendacionDto req, CancellationToken ct = default)
    {
        // HU19: latencia_ms de una recomendación es lo que espera el estudiante (recuperar + IA), no solo la IA.
        var reloj = Stopwatch.StartNew();
        var entrada = LimpiarEntrada(req.Entrada);

        var puente = (await _puentes.ListarPorUsuarioAsync(usuarioId, ct)).FirstOrDefault(p => p.Id == req.PuenteId)
                     ?? throw new ReglaNegocioException("NO_ENCONTRADO", "El puente no existe.");
        var minutosDisponibles = (int)(puente.HoraFin - puente.HoraInicio).TotalMinutes;

        var rechazo = ReglasSeguridadIa.Evaluar(entrada);
        if (rechazo is not null)
        {
            await _repo.GuardarAsync(new LogAuditoriaRag
            {
                UsuarioId = usuarioId,
                Tipo = LogAuditoriaRag.TipoRecomendacion,
                PuenteId = puente.Id,
                TiempoDisponibleMin = (short)minutosDisponibles,
                EntradaUsuario = entrada,
                Estado = LogAuditoriaRag.EstadoRechazadoSeguridad,
                MotivoRechazo = rechazo.Categoria,
                CreatedAt = DateTime.UtcNow
            }, [], ct);
            throw new ReglaNegocioException(CodigoSolicitudNoPermitida, rechazo.Mensaje);
        }

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

        if (candidatos.Count == 0) return await SinResultadosAsync(log, ct);

        var prompt = PromptRecomendacion.Construir(candidatos, minutosDisponibles, entrada);
        log.PromptEnviado = prompt.ComoTexto();

        RespuestaAsistente respuesta;
        // HU19: la IA tiene un presupuesto; si no contesta a tiempo o falla, responde el plan B (los más cercanos).
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limite.CancelAfter(_presupuestoIa);
        try
        {
            respuesta = await _asistente.GenerarAsync(prompt, limite.Token);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return await RespaldoAsync(log, candidatos, minutosDisponibles, reloj, ct);
        }

        log.Modelo = respuesta.Modelo;
        log.RespuestaLlm = respuesta.TextoCrudo;
        log.LatenciaMs = (int)reloj.ElapsedMilliseconds;

        var sugerencias = respuesta.Sugerencias.Select(s => (s.LugarId, s.Explicacion)).ToList();
        var recuperados = candidatos.ToDictionary(l => l.LugarId, l => l.Nombre);
        var validas = ReglasRecomendacion.FiltrarSoloRecuperados(sugerencias, recuperados);

        if (validas.Count == 0)
        {
            if (respuesta.Sugerencias.Count > 0)
            {
                log.Estado = LogAuditoriaRag.EstadoRechazadoSeguridad;
                log.MotivoRechazo = ReglasRecomendacion.MotivoDescarte(sugerencias, recuperados);
            }
            return await SinResultadosAsync(log, ct);
        }

        log.Estado = LogAuditoriaRag.EstadoOk;
        var recomendaciones = validas.Select(v => new Recomendacion
        {
            LugarId = v.LugarId,
            Explicacion = v.Explicacion,
            LugarRecuperado = log.LugaresRecuperados.Single(l => l.LugarId == v.LugarId)
        }).ToList();

        await _repo.GuardarAsync(log, recomendaciones, ct);

        var porId = candidatos.ToDictionary(l => l.LugarId);
        return new RespuestaRecomendacionDto(log.Id, EstadoRespuestaOk, null,
            recomendaciones.Select(r => ADto(r, porId[r.LugarId])).ToList());
    }

    private async Task<RespuestaRecomendacionDto> RespaldoAsync(LogAuditoriaRag log, List<LugarCandidato> candidatos, int minutosDisponibles,
        Stopwatch reloj, CancellationToken ct)
    {
        var elegidos = ReglasRendimiento.Respaldo(
            candidatos.Select(c => new ReglasRendimiento.Candidato(c.LugarId, c.TiempoTrasladoMin, c.DistanciaM, c.DuracionEstimadaMin)),
            minutosDisponibles);

        log.Estado = LogAuditoriaRag.EstadoOk;
        log.Modelo = ReglasRendimiento.ModeloRespaldo;
        log.LatenciaMs = (int)reloj.ElapsedMilliseconds;
        var recomendaciones = elegidos.Select(e => new Recomendacion
        {
            LugarId = e.LugarId,
            Explicacion = e.Explicacion,
            LugarRecuperado = log.LugaresRecuperados.Single(l => l.LugarId == e.LugarId)
        }).ToList();

        await _repo.GuardarAsync(log, recomendaciones, ct);

        var porId = candidatos.ToDictionary(l => l.LugarId);
        return new RespuestaRecomendacionDto(log.Id, EstadoRespuestaOk, ReglasRendimiento.MensajeRespaldo,
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

        var saneada = ReglasSeguridadIa.Sanear(limpia);
        return saneada.Length == 0 ? null : saneada;
    }

    private static RecomendacionDto ADto(Recomendacion r, LugarCandidato l) =>
        new(r.Id, l.LugarId, l.Nombre, l.Categoria, l.DistanciaM, l.TiempoTrasladoMin, l.DuracionEstimadaMin, r.Explicacion, r.Feedback, l.FotoUrl,
            l.Puntuacion, l.Votos, l.Validado);

    public async Task<FeedbackGuardadoDto> GuardarFeedbackAsync(int usuarioId, long recomendacionId, string? feedback, CancellationToken ct = default)
    {
        var valor = ReglasFeedback.Normalizar(feedback)
            ?? throw new ReglaNegocioException("VALIDACION", ReglasFeedback.MensajeInvalido);

        var recomendacion = await _repo.ObtenerDeUsuarioAsync(recomendacionId, usuarioId, ct)
            ?? throw new ReglaNegocioException("NO_ENCONTRADO", "La recomendación no existe.");

        if (ReglasFeedback.CambiaElVoto(recomendacion.Feedback, valor))
            await _repo.GuardarFeedbackAsync(recomendacion, valor, ct);
        return new FeedbackGuardadoDto(recomendacion.Id, recomendacion.Feedback!, recomendacion.FechaFeedback!.Value);
    }
}
