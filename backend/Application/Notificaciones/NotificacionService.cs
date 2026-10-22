using Breaku.Application.Horarios;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Notificaciones;

/// <summary>Casos de uso de HU4: avisar al estudiante cuando empieza un puente y administrar sus avisos.</summary>
public class NotificacionService
{
    private const int MaximoListado = 20;

    private readonly INotificacionRepository _repo;
    public NotificacionService(INotificacionRepository repo) => _repo = repo;

    /// <summary>
    /// Crea un aviso por cada puente que acaba de empezar (dentro de la ventana de envío). Lo llama el
    /// worker periódicamente. Los usuarios con notificaciones desactivadas quedan fuera, pero sus puentes
    /// se siguen detectando igual (criterio 5). Devuelve cuántos avisos se enviaron.
    /// </summary>
    public async Task<int> EnviarAvisosAsync(DateTime ahoraUtc, CancellationToken ct = default)
    {
        var ahoraLocal = ReglasNotificacion.AHoraLocal(ahoraUtc);
        var fecha = DateOnly.FromDateTime(ahoraLocal);
        var dia = ReglasNotificacion.DiaSemana(fecha);
        if (dia == 0) return 0; // domingo: no hay clases ni puentes

        var hora = TimeOnly.FromDateTime(ahoraLocal);
        var desde = hora.ToTimeSpan() >= ReglasNotificacion.VentanaEnvio
            ? hora.Add(-ReglasNotificacion.VentanaEnvio)
            : TimeOnly.MinValue;

        var puentes = await _repo.ListarPuentesPorAvisarAsync(dia, fecha, desde, hora, ct);

        var enviados = 0;
        foreach (var p in puentes)
        {
            if (!ReglasNotificacion.DebeAvisarse(p.HoraInicio, hora)) continue;      // ni antes ni después (criterio 2)
            if (!ReglasPuente.EsPuente(p.HoraInicio, p.HoraFin)) continue;            // solo puentes de 45 min o más (criterio 1)

            var aviso = new Notificacion
            {
                UsuarioId = p.UsuarioId,
                Tipo = Notificacion.TipoPuenteDisponible,
                PuenteFecha = fecha,
                PuenteHoraInicio = p.HoraInicio,
                PuenteHoraFin = p.HoraFin,
                Mensaje = ReglasNotificacion.Mensaje(p.HoraInicio, p.HoraFin),
                CreatedAt = ahoraUtc
            };

            // Si ya había aviso para este puente no se repite (criterio 3).
            if (await _repo.AgregarAsync(aviso, ct)) enviados++;
        }

        return enviados;
    }

    /// <summary>
    /// Desactiva los avisos cuyo puente ya no está entre los puentes actuales del usuario (criterio 4).
    /// Lo llama PuenteService después de recalcular los puentes.
    /// </summary>
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
        // MarcarLeidaAsync filtra por usuario: nadie marca avisos ajenos (BOLA).
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
            n.Leida, n.CreatedAt);
}
