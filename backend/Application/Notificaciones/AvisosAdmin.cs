using Breaku.Application.TiempoReal;
using Breaku.Domain;
using Microsoft.Extensions.Logging;

namespace Breaku.Application.Notificaciones;

/// <summary>Lo que el admin tiene que revisar y a qué apunta el aviso (un reporte o un evento).</summary>
public enum ObjetoAvisoAdmin { Reporte, Evento }

/// <summary>Guarda los avisos de la campanita de los administradores.</summary>
public interface IAvisosAdminRepository
{
    /// <summary>
    /// Un aviso por cada admin activo (si ya tenía uno por ese mismo objeto, vuelve a quedar sin leer con el texto nuevo).
    /// Devuelve a quiénes les llegó.
    /// </summary>
    Task<IReadOnlyList<int>> AvisarAsync(string tipo, ObjetoAvisoAdmin objeto, int objetoId, string mensaje, CancellationToken ct = default);

    /// <summary>Marca como leídos los avisos de ese objeto de todos los admins. Devuelve a quiénes les cambió algo.</summary>
    Task<IReadOnlyList<int>> CerrarAsync(string tipo, ObjetoAvisoAdmin objeto, int objetoId, CancellationToken ct = default);

    /// <summary>
    /// HU32: el estudiante y cuántos intentos suyos (escritos por él, no respuestas del modelo) se bloquearon por seguridad
    /// desde <paramref name="desdeUtc"/>. <c>null</c> si no existe.
    /// </summary>
    Task<CandidatoAlerta?> IntentosBloqueadosAsync(int usuarioId, DateTime desdeUtc, CancellationToken ct = default);

    /// <summary>
    /// HU32: quien creó la comunidad o el evento del reporte, y cuántas personas distintas tienen reportes PENDIENTES sobre
    /// sus comunidades o eventos. <c>null</c> si el reporte no apunta a una comunidad o un evento.
    /// </summary>
    Task<CandidatoAlerta?> CreadorReportadoAsync(int reporteId, CancellationToken ct = default);

    /// <summary>
    /// HU32: una alerta sobre <paramref name="usuarioObjetivoId"/> para cada admin activo. Si ya la tenía (uq_notif_objetivo),
    /// se actualiza el mensaje y vuelve a quedar sin leer. Devuelve a quiénes les llegó.
    /// </summary>
    Task<IReadOnlyList<int>> AlertarAsync(string tipo, int usuarioObjetivoId, string mensaje, CancellationToken ct = default);

    /// <summary>
    /// HU32: las alertas activas, una fila por estudiante y tipo (la más reciente; cada admin tiene su copia, se cuenta una).
    /// </summary>
    Task<List<FilaAlerta>> ListarAlertasActivasAsync(CancellationToken ct = default);

    /// <summary>
    /// HU32: el admin ya decidió sobre el estudiante (lo sancionó o descartó la alerta): sus alertas dejan de estar activas
    /// para todos los admins. Si vuelve a pasar, la alerta se reactiva sola. Devuelve a qué admins les cambió algo.
    /// </summary>
    Task<IReadOnlyList<int>> ResolverAlertasAsync(int usuarioObjetivoId, CancellationToken ct = default);
}

/// <summary>HU32: una alerta activa tal como la lee la base.</summary>
public sealed record FilaAlerta(int UsuarioId, string Nombre, string Correo, bool Activo, string Tipo, string Mensaje, DateTime FechaUtc);

/// <summary>HU32: una alerta en la pestaña Alertas del panel o en el detalle del estudiante.</summary>
public sealed record AlertaDto(string Tipo, string Mensaje, DateTime Fecha);

/// <summary>HU32: un estudiante con alertas activas, de la más reciente a la más antigua.</summary>
public sealed record AlertaEstudianteDto(int UsuarioId, string Nombre, string Correo, bool Activo, DateTime Ultima, List<AlertaDto> Alertas);

/// <summary>HU32: el estudiante del que se podría avisar y la cifra que decide si se avisa.</summary>
/// <param name="DesdeUltimaAdvertencia">
/// Solo en intentos bloqueados: cuántos hubo desde su última advertencia vigente (no levantada). <c>null</c> si nunca lo
/// advirtieron.
/// </param>
public sealed record CandidatoAlerta(int UsuarioId, string Nombre, bool EsAdmin, int Cantidad, int? DesdeUltimaAdvertencia = null);

/// <summary>
/// Avisos para el administrador: reportes nuevos y eventos que esperan revisión. Nunca rompen la operación del
/// estudiante (si la base todavía no tiene la migración 030 o falla el canal en vivo, el reporte o el evento igual se
/// guardan; el admin los sigue viendo en el panel).
/// </summary>
public interface IAvisosAdmin
{
    Task ReporteNuevoAsync(int reporteId, string motivo, string? descripcion, CancellationToken ct = default);
    Task ReporteRevisadoAsync(int reporteId, CancellationToken ct = default);
    Task EventoPorRevisarAsync(int eventoId, string titulo, string? motivo, CancellationToken ct = default);
    Task EventoRevisadoAsync(int eventoId, CancellationToken ct = default);

    /// <summary>HU32: se bloqueó por seguridad algo que escribió el estudiante. Desde el tercero en 24 h, avisa a los admins.</summary>
    Task IntentoBloqueadoAsync(int usuarioId, CancellationToken ct = default);

    /// <summary>HU32: se creó un reporte. Si su creador ya acumula reportes de 3 o más personas, avisa a los admins.</summary>
    Task ReportesAcumuladosAsync(int reporteId, CancellationToken ct = default);

    /// <summary>HU32: el admin sancionó al estudiante: sus alertas quedan resueltas (no rompe la sanción si falla).</summary>
    Task AlertasResueltasAsync(int usuarioId, CancellationToken ct = default);
}

public sealed class AvisosAdminService : IAvisosAdmin
{
    private readonly IAvisosAdminRepository _repo;
    private readonly ITiempoReal _tiempoReal;
    private readonly TimeProvider _reloj;
    private readonly ILogger<AvisosAdminService>? _logger;

    public AvisosAdminService(IAvisosAdminRepository repo, ITiempoReal tiempoReal) : this(repo, tiempoReal, TimeProvider.System) { }

    public AvisosAdminService(IAvisosAdminRepository repo, ITiempoReal tiempoReal, ILogger<AvisosAdminService> logger)
        : this(repo, tiempoReal, TimeProvider.System, logger) { }

    public AvisosAdminService(IAvisosAdminRepository repo, ITiempoReal tiempoReal, TimeProvider reloj,
        ILogger<AvisosAdminService>? logger = null)
    {
        _repo = repo;
        _tiempoReal = tiempoReal;
        _reloj = reloj;
        _logger = logger;
    }

    public Task IntentoBloqueadoAsync(int usuarioId, CancellationToken ct = default)
        => Seguro(async () =>
        {
            var desde = ReglasAlertasAdmin.InicioVentana(_reloj.GetUtcNow().UtcDateTime);
            var c = await _repo.IntentosBloqueadosAsync(usuarioId, desde, ct);
            if (c is null || !ReglasAlertasAdmin.AlertarPorIntentos(c.Cantidad, c.EsAdmin)) return [];
            return await _repo.AlertarAsync(Notificacion.TipoAlertaUsuarioSospechoso, c.UsuarioId,
                ReglasAlertasAdmin.MensajeSospechoso(c.Nombre, c.Cantidad, c.DesdeUltimaAdvertencia), ct);
        }, ct);

    public Task AlertasResueltasAsync(int usuarioId, CancellationToken ct = default)
        => Seguro(() => _repo.ResolverAlertasAsync(usuarioId, ct), ct);

    public Task ReportesAcumuladosAsync(int reporteId, CancellationToken ct = default)
        => Seguro(async () =>
        {
            var c = await _repo.CreadorReportadoAsync(reporteId, ct);
            if (c is null || !ReglasAlertasAdmin.AlertarPorReportes(c.Cantidad, c.EsAdmin)) return [];
            return await _repo.AlertarAsync(Notificacion.TipoAlertaReportesAcumulados, c.UsuarioId,
                ReglasAlertasAdmin.MensajeReportes(c.Nombre, c.Cantidad), ct);
        }, ct);

    public Task ReporteNuevoAsync(int reporteId, string motivo, string? descripcion, CancellationToken ct = default)
        => Seguro(() => _repo.AvisarAsync(Notificacion.TipoReporteNuevo, ObjetoAvisoAdmin.Reporte, reporteId,
            ReglasAvisosAdmin.MensajeReporteNuevo(motivo, descripcion), ct), ct);

    public Task ReporteRevisadoAsync(int reporteId, CancellationToken ct = default)
        => Seguro(() => _repo.CerrarAsync(Notificacion.TipoReporteNuevo, ObjetoAvisoAdmin.Reporte, reporteId, ct), ct);

    public Task EventoPorRevisarAsync(int eventoId, string titulo, string? motivo, CancellationToken ct = default)
        => Seguro(() => _repo.AvisarAsync(Notificacion.TipoEventoPorRevisar, ObjetoAvisoAdmin.Evento, eventoId,
            ReglasAvisosAdmin.MensajeEventoPorRevisar(titulo, motivo), ct), ct);

    public Task EventoRevisadoAsync(int eventoId, CancellationToken ct = default)
        => Seguro(() => _repo.CerrarAsync(Notificacion.TipoEventoPorRevisar, ObjetoAvisoAdmin.Evento, eventoId, ct), ct);

    private async Task Seguro(Func<Task<IReadOnlyList<int>>> accion, CancellationToken ct)
    {
        try
        {
            var admins = await accion();
            if (admins.Count > 0) await _tiempoReal.AvisosCambiaronAsync(admins, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // El aviso al admin es un extra: lo importante (el reporte, el evento, la decisión, el rechazo) ya quedó
            // guardado. Queda en el log para poder rastrearlo.
            _logger?.LogWarning(ex, "No se pudo guardar un aviso para los administradores.");
        }
    }
}

/// <summary>
/// HU32: la pestaña «Alertas» del panel: los estudiantes con alertas activas, para revisar y decidir. Descartar no sanciona:
/// solo saca la alerta de la lista (y de la campanita) hasta que vuelva a pasar.
/// </summary>
public sealed class AlertasAdminService
{
    private readonly IAvisosAdminRepository _repo;
    private readonly ITiempoReal _tiempoReal;

    public AlertasAdminService(IAvisosAdminRepository repo, ITiempoReal tiempoReal)
    {
        _repo = repo;
        _tiempoReal = tiempoReal;
    }

    public async Task<List<AlertaEstudianteDto>> ListarAsync(CancellationToken ct = default)
        => (await _repo.ListarAlertasActivasAsync(ct))
            .GroupBy(f => f.UsuarioId)
            .Select(g =>
            {
                var alertas = g.OrderByDescending(f => f.FechaUtc)
                    .Select(f => new AlertaDto(f.Tipo, f.Mensaje, DateTime.SpecifyKind(f.FechaUtc, DateTimeKind.Utc)))
                    .ToList();
                var p = g.First();
                return new AlertaEstudianteDto(p.UsuarioId, p.Nombre, p.Correo, p.Activo, alertas[0].Fecha, alertas);
            })
            .OrderByDescending(a => a.Ultima)
            .ToList();

    public async Task DescartarAsync(int usuarioId, CancellationToken ct = default)
    {
        var admins = await _repo.ResolverAlertasAsync(usuarioId, ct);
        if (admins.Count > 0) await _tiempoReal.AvisosCambiaronAsync(admins, ct);
    }
}

