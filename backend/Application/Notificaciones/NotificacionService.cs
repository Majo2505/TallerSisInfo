using Breaku.Application.Horarios;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Notificaciones;

public class NotificacionService
{
    private const int MaximoListado = 20;

    private readonly INotificacionRepository _repo;
    public NotificacionService(INotificacionRepository repo) => _repo = repo;

    public async Task<int> EnviarAvisosAsync(DateTime ahoraUtc, CancellationToken ct = default)
    {
        var ahoraLocal = ReglasNotificacion.AHoraLocal(ahoraUtc);
        var fecha = DateOnly.FromDateTime(ahoraLocal);
        var dia = ReglasNotificacion.DiaSemana(fecha);
        if (dia == 0) return 0;

        var hora = TimeOnly.FromDateTime(ahoraLocal);
        var desde = hora.ToTimeSpan() >= ReglasNotificacion.VentanaEnvio
            ? hora.Add(-ReglasNotificacion.VentanaEnvio)
            : TimeOnly.MinValue;

        var puentes = await _repo.ListarPuentesPorAvisarAsync(dia, fecha, desde, hora, ct);

        var enviados = 0;
        foreach (var p in puentes)
        {
            if (!ReglasNotificacion.DebeAvisarse(p.HoraInicio, hora)) continue;
            if (!ReglasPuente.EsPuente(p.HoraInicio, p.HoraFin)) continue;

            var despuesDeClases = p.Tipo == Puente.TipoDespuesDeClases;
            var aviso = new Notificacion
            {
                UsuarioId = p.UsuarioId,
                Tipo = despuesDeClases ? Notificacion.TipoDespuesDeClases : Notificacion.TipoPuenteDisponible,
                PuenteFecha = fecha,
                PuenteHoraInicio = p.HoraInicio,
                PuenteHoraFin = p.HoraFin,
                Mensaje = despuesDeClases
                    ? ReglasNotificacion.MensajeDespuesDeClases(p.HoraInicio, p.HoraFin)
                    : ReglasNotificacion.Mensaje(p.HoraInicio, p.HoraFin),
                CreatedAt = ahoraUtc
            };

            if (await _repo.AgregarAsync(aviso, ct)) enviados++;
        }

        return enviados;
    }

    public async Task DesactivarSinPuenteAsync(int usuarioId, IReadOnlyList<Puente> puentesActuales, CancellationToken ct = default)
    {
        var activas = await _repo.ListarActivasDePuenteAsync(usuarioId, ct);
        var huerfanas = activas
            .Where(n => !puentesActuales.Any(p =>
                p.DiaSemana == ReglasNotificacion.DiaSemana(n.PuenteFecha!.Value) &&
                p.HoraInicio == n.PuenteHoraInicio &&
                p.HoraFin == n.PuenteHoraFin))
            .Select(n => n.Id)
            .ToList();

        if (huerfanas.Count > 0) await _repo.DesactivarAsync(huerfanas, ct);
    }

    public async Task<List<NotificacionDto>> ListarAsync(int usuarioId, CancellationToken ct = default)
    {
        var notificaciones = await _repo.ListarActivasAsync(usuarioId, MaximoListado, ct);
        return notificaciones.Select(ADto).ToList();
    }

    public async Task MarcarLeidaAsync(int usuarioId, long id, CancellationToken ct = default)
    {
        if (!await _repo.MarcarLeidaAsync(id, usuarioId, ct))
            throw new ReglaNegocioException("NO_ENCONTRADO", "La notificación no existe.");
    }

    public async Task<ConfiguracionNotificacionesDto> ObtenerConfiguracionAsync(int usuarioId, CancellationToken ct = default)
    {
        var activas = await _repo.ObtenerPreferenciaAsync(usuarioId, ct)
                      ?? throw new ReglaNegocioException("NO_ENCONTRADO", "El usuario no existe.");
        return new ConfiguracionNotificacionesDto(activas);
    }

    public async Task<ConfiguracionNotificacionesDto> GuardarConfiguracionAsync(int usuarioId, ConfiguracionNotificacionesDto req, CancellationToken ct = default)
    {
        await _repo.GuardarPreferenciaAsync(usuarioId, req.NotificacionesActivas, ct);
        return req;
    }

    private static NotificacionDto ADto(Notificacion n) =>
        new(n.Id, n.Tipo, n.Mensaje,
            n.PuenteFecha?.ToString("yyyy-MM-dd"),
            n.PuenteHoraInicio?.ToString("HH:mm:ss"),
            n.PuenteHoraFin?.ToString("HH:mm:ss"),
            n is { PuenteHoraInicio: { } ini, PuenteHoraFin: { } fin } ? ReglasNotificacion.DuracionMinutos(ini, fin) : null,
            n.Leida, n.CreatedAt, n.ComunidadId);
}
