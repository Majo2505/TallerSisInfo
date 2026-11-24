using Breaku.Application.Horarios;
using Breaku.Application.Notificaciones;
using Breaku.Application.TiempoReal;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Admin;

/// <summary>HU35: una comunidad en el panel del admin.</summary>
public sealed record ComunidadAdminDto(
    int Id,
    string Actividad,
    int CreadorId,
    string Creador,
    DateTimeOffset Inicio,
    DateTimeOffset Fin,
    int Confirmados,
    int MaxParticipantes,
    bool Privada,
    string Estado,
    int ReportesPendientes,
    string? MotivoBaja,
    DateTimeOffset? FechaBaja);

/// <summary>Quitar una comunidad: motivo obligatorio; con <c>ReporteId</c>, ese reporte queda RESUELTO en la misma transacción.</summary>
public sealed record QuitarComunidadRequest(string? Motivo, int? ReporteId = null);

/// <summary>Una comunidad tal como la lee la base para el panel.</summary>
public sealed record FilaComunidadAdmin(
    int Id, string Actividad, int CreadorId, string Creador, DateTime InicioUtc, DateTime FinUtc, int ConfirmadosEnTabla,
    bool CreadorEnTabla, int MaxParticipantes, bool Privada, string Estado, int ReportesPendientes, string? MotivoBaja,
    DateTime? FechaBajaUtc);

public enum ResultadoModeracionComunidad { Aplicado, NoEncontrada, EstadoInvalido, YaTermino, ReporteNoCorresponde, ReporteYaRevisado }

/// <summary>
/// Lo que pasó al quitar o restaurar: a quiénes se avisó (para refrescar sus campanitas en vivo), cuántos van (para las
/// pantallas abiertas) y, si venía de un reporte, su id (para cerrar el aviso del reporte en la campanita de los admins).
/// </summary>
public sealed record CambioComunidad(ResultadoModeracionComunidad Resultado, IReadOnlyList<int> Avisados, int Confirmados, int? ReporteId);

public interface IModeracionComunidadRepository
{
    /// <summary>Las ACTIVAS que no terminaron (por inicio) o las BAJADAS (la última quitada primero), hasta <paramref name="maximo"/>.</summary>
    Task<List<FilaComunidadAdmin>> ListarAsync(string estado, DateTime ahoraUtc, int maximo, CancellationToken ct = default);

    /// <summary>
    /// En una transacción: si sigue ACTIVA, la deja BAJADA con motivo y fecha, avisa (COMUNIDAD_BAJADA) a quien la creó y a
    /// los CONFIRMADO, apaga sus invitaciones pendientes y, con <paramref name="reporteId"/>, deja el reporte RESUELTO y avisa
    /// a quien reportó (REPORTE_RESUELTO). No borra nada ni sanciona.
    /// </summary>
    Task<CambioComunidad> QuitarAsync(int comunidadId, int adminId, string motivo, Func<string, string> aviso, int? reporteId,
        string respuestaReporte, DateTime ahoraUtc, CancellationToken ct = default);

    /// <summary>En una transacción: si está BAJADA, vuelve a ACTIVA sin motivo ni fecha y avisa (COMUNIDAD_RESTAURADA).</summary>
    Task<CambioComunidad> RestaurarAsync(int comunidadId, Func<string, string> aviso, DateTime ahoraUtc, CancellationToken ct = default);
}

/// <summary>HU35: el admin quita y restaura comunidades. Quitar no sanciona a quien la creó.</summary>
public sealed class ModeracionComunidadesService
{
    public const int Maximo = 100;

    private readonly IModeracionComunidadRepository _repo;
    private readonly TimeProvider _reloj;
    private readonly ITiempoReal? _tiempoReal;
    private readonly IAvisosAdmin? _avisos;

    public ModeracionComunidadesService(IModeracionComunidadRepository repo, ITiempoReal tiempoReal, IAvisosAdmin avisos)
        : this(repo, TimeProvider.System, tiempoReal, avisos) { }

    public ModeracionComunidadesService(IModeracionComunidadRepository repo, TimeProvider reloj, ITiempoReal? tiempoReal = null,
        IAvisosAdmin? avisos = null)
    {
        _repo = repo;
        _reloj = reloj;
        _tiempoReal = tiempoReal;
        _avisos = avisos;
    }

    public async Task<List<ComunidadAdminDto>> ListarAsync(string? estado, CancellationToken ct = default)
    {
        var pedido = (estado ?? ComunidadEfimera.EstadoActiva).Trim().ToUpperInvariant();
        if (pedido is not (ComunidadEfimera.EstadoActiva or ComunidadEfimera.EstadoBajada))
            throw new ReglaNegocioException(ReglasModeracionComunidad.ErrorValidacion, "El estado tiene que ser ACTIVA o BAJADA.");

        return (await _repo.ListarAsync(pedido, _reloj.GetUtcNow().UtcDateTime, Maximo, ct))
            .Select(f => new ComunidadAdminDto(
                f.Id, f.Actividad, f.CreadorId, f.Creador, Local(f.InicioUtc), Local(f.FinUtc),
                ReglasDescubrir.Confirmados(f.ConfirmadosEnTabla, f.CreadorEnTabla), f.MaxParticipantes, f.Privada, f.Estado,
                f.ReportesPendientes, f.MotivoBaja, f.FechaBajaUtc is { } fb ? Local(fb) : null))
            .ToList();
    }

    public async Task QuitarAsync(int comunidadId, int adminId, QuitarComunidadRequest request, CancellationToken ct = default)
    {
        var (motivo, error) = ReglasModeracionComunidad.ValidarMotivo(ReglasSeguridadIa.Sanear(request.Motivo ?? ""));
        if (error is not null) throw new ReglaNegocioException(ReglasModeracionComunidad.ErrorValidacion, error);

        var cambio = await _repo.QuitarAsync(comunidadId, adminId, motivo!,
            actividad => ReglasModeracionComunidad.AvisoBajada(actividad, motivo!), request.ReporteId,
            ReglasModeracionComunidad.RespuestaReporte(motivo!), _reloj.GetUtcNow().UtcDateTime, ct);
        LanzarSiNoSeAplico(cambio.Resultado, ReglasModeracionComunidad.MensajeNoActiva);
        await AvisarAsync(comunidadId, ComunidadEfimera.EstadoBajada, cambio, ct);
    }

    public async Task RestaurarAsync(int comunidadId, CancellationToken ct = default)
    {
        var cambio = await _repo.RestaurarAsync(comunidadId, ReglasModeracionComunidad.AvisoRestaurada,
            _reloj.GetUtcNow().UtcDateTime, ct);
        LanzarSiNoSeAplico(cambio.Resultado, ReglasModeracionComunidad.MensajeNoBajada);
        await AvisarAsync(comunidadId, ComunidadEfimera.EstadoActiva, cambio, ct);
    }

    private static void LanzarSiNoSeAplico(ResultadoModeracionComunidad resultado, string mensajeEstado)
    {
        switch (resultado)
        {
            case ResultadoModeracionComunidad.NoEncontrada:
                throw new ReglaNegocioException(ReglasModeracionComunidad.ErrorNoEncontrada, ReglasModeracionComunidad.MensajeNoEncontrada);
            case ResultadoModeracionComunidad.EstadoInvalido:
                throw new ReglaNegocioException(ReglasModeracionComunidad.ErrorEstado, mensajeEstado);
            case ResultadoModeracionComunidad.YaTermino:
                throw new ReglaNegocioException(ReglasModeracionComunidad.ErrorEstado, ReglasModeracionComunidad.MensajeYaTermino);
            case ResultadoModeracionComunidad.ReporteNoCorresponde:
                throw new ReglaNegocioException(ReglasModeracionComunidad.ErrorValidacion, ReglasModeracionComunidad.MensajeReporteNoCorresponde);
            case ResultadoModeracionComunidad.ReporteYaRevisado:
                throw new ReglaNegocioException(ReglasModeracionComunidad.ErrorReporteYaRevisado, ReglasModeracionComunidad.MensajeReporteYaRevisado);
        }
    }

    /// <summary>Ya guardado: campanitas en vivo, pantallas abiertas de esa comunidad y Descubrir de su sede. Nada de esto rompe la acción.</summary>
    private async Task AvisarAsync(int comunidadId, string estado, CambioComunidad cambio, CancellationToken ct)
    {
        if (_tiempoReal is not null)
        {
            if (cambio.Avisados.Count > 0) await _tiempoReal.AvisosCambiaronAsync(cambio.Avisados, ct);
            await _tiempoReal.ComunidadActualizadaAsync(comunidadId, estado, cambio.Confirmados, ct);
            await _tiempoReal.ComunidadCreadaAsync(comunidadId, ct); // refresca Descubrir de su sede (aparece o desaparece)
        }
        if (_avisos is not null && cambio.ReporteId is int reporteId) await _avisos.ReporteRevisadoAsync(reporteId, ct);
    }

    private static DateTimeOffset Local(DateTime utc)
        => new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToOffset(ReglasNotificacion.DesfaseBolivia);
}
