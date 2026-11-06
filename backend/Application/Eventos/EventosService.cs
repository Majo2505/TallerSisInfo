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
        var horarios = await _horarios.ListarPorUsuarioAsync(usuarioId, ct);
        var siguienteClase = horarios
            .Where(h => h.Asiste && h.DiaSemana is >= 1 and <= 6)
            .Select(h =>
            {
                var diasHasta = (h.DiaSemana - (int)ahoraLocal.DayOfWeek + 7) % 7;
                var fecha = ahoraLocal.Date.AddDays(diasHasta);
                var inicio = fecha + h.HoraInicio.ToTimeSpan();
                return inicio <= ahoraLocal ? inicio.AddDays(7) : inicio;
            })
            .OrderBy(inicio => inicio)
            .FirstOrDefault();
        if (siguienteClase == default) return [];

        var limiteLocal = siguienteClase;
        var limiteUtc = DateTime.SpecifyKind(limiteLocal - ReglasNotificacion.DesfaseBolivia, DateTimeKind.Utc);
        var puentes = await _puentes.ListarPorUsuarioAsync(usuarioId, ct);
        var eventos = await _eventos.ListarVigentesAsync(ahoraUtc, limiteUtc, ct);

        return eventos
            .Where(e =>
            {
                var inicioLocal = ReglasNotificacion.AHoraLocal(e.Inicio);
                var finLocal = ReglasNotificacion.AHoraLocal(e.Fin);
                return finLocal > ahoraLocal && inicioLocal < limiteLocal &&
                    !puentes.Any(p => p.DiaSemana == (int)inicioLocal.DayOfWeek &&
                        p.HoraInicio < TimeOnly.FromDateTime(finLocal) &&
                        p.HoraFin > TimeOnly.FromDateTime(inicioLocal));
            })
            .Select(e => new EventoRecomendadoDto(e.Id, e.Titulo, e.Descripcion,
                ReglasNotificacion.AHoraLocal(e.Inicio).ToString("HH:mm"),
                ReglasNotificacion.AHoraLocal(e.Fin).ToString("HH:mm"), e.Lugar, e.FuenteUrl))
            .ToList();
    }
}
