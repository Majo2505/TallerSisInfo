using Breaku.Application.Horarios;
using Breaku.Application.Puentes;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Eventos;

public sealed class EventosService
{
    private readonly IEventosRepository _eventos;
    private readonly IHorarioRepository _horarios;
    private readonly IPuenteRepository _puentes;
    private readonly IEventosPublicosClient _cliente;
    private readonly TimeProvider _reloj;

    public EventosService(IEventosRepository eventos, IHorarioRepository horarios, IPuenteRepository puentes,
        IEventosPublicosClient cliente) : this(eventos, horarios, puentes, cliente, TimeProvider.System) { }

    public EventosService(IEventosRepository eventos, IHorarioRepository horarios, IPuenteRepository puentes,
        IEventosPublicosClient cliente, TimeProvider reloj)
    {
        _eventos = eventos;
        _horarios = horarios;
        _puentes = puentes;
        _cliente = cliente;
        _reloj = reloj;
    }

    public async Task RefrescarAsync(CancellationToken ct = default)
    {
        var ahora = _reloj.GetUtcNow().UtcDateTime;
        var nuevos = await _cliente.ObtenerAsync(ct);
        await _eventos.UpsertAsync(nuevos, ahora, ct);
    }

    public async Task<List<EventoRecomendadoDto>> ListarRecomendadosAsync(int usuarioId, CancellationToken ct = default)
    {
        var ahoraUtc = _reloj.GetUtcNow().UtcDateTime;
        var ahoraLocal = ReglasNotificacion.AHoraLocal(ahoraUtc);
        var siguienteClase = (await _horarios.ListarPorUsuarioAsync(usuarioId, ct))
            .Where(h => h.Asiste && h.DiaSemana == (int)ahoraLocal.DayOfWeek && h.HoraInicio > TimeOnly.FromDateTime(ahoraLocal))
            .OrderBy(h => h.HoraInicio)
            .Select(h => h.HoraInicio)
            .FirstOrDefault();
        if (siguienteClase == default) return [];

        var limiteLocal = ahoraLocal.Date + siguienteClase.ToTimeSpan();
        var limiteUtc = DateTime.SpecifyKind(limiteLocal - ReglasNotificacion.DesfaseBolivia, DateTimeKind.Utc);
        var puentes = await _puentes.ListarPorUsuarioAsync(usuarioId, ct);
        var eventos = await _eventos.ListarVigentesAsync(ahoraUtc, limiteUtc, ct);
        var dia = (int)ahoraLocal.DayOfWeek;

        return eventos
            .Where(e => ReglasNotificacion.AHoraLocal(e.Inicio).DayOfWeek == ahoraLocal.DayOfWeek)
            .Where(e => !puentes.Any(p => p.DiaSemana == dia &&
                p.HoraInicio < TimeOnly.FromDateTime(e.Fin) &&
                p.HoraFin > TimeOnly.FromDateTime(e.Inicio)))
            .Select(e => new EventoRecomendadoDto(e.Id, e.Titulo, e.Descripcion,
                ReglasNotificacion.AHoraLocal(e.Inicio).ToString("HH:mm"),
                ReglasNotificacion.AHoraLocal(e.Fin).ToString("HH:mm"), e.Lugar, e.FuenteUrl))
            .ToList();
    }
}
