using Breaku.Application.Puentes;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Eventos;

public sealed class EventosService
{
    private readonly IEventosRepository _eventos;
    private readonly IPuenteRepository _puentes;
    private readonly IEventosPublicosClient _cliente;
    private readonly TimeProvider _reloj;

    public EventosService(IEventosRepository eventos, IPuenteRepository puentes,
        IEventosPublicosClient cliente) : this(eventos, puentes, cliente, TimeProvider.System) { }

    public EventosService(IEventosRepository eventos, IPuenteRepository puentes,
        IEventosPublicosClient cliente, TimeProvider reloj)
    {
        _eventos = eventos;
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
        var puentes = await _puentes.ListarPorUsuarioAsync(usuarioId, ct);
        if (puentes.Count == 0) return [];

        var eventos = await _eventos.ListarProximosAsync(ahoraUtc, ct);
        return eventos
            .Select(e => (Evento: e, Inicio: ReglasNotificacion.AHoraLocal(e.Inicio), Fin: ReglasNotificacion.AHoraLocal(e.Fin)))
            .Where(x => ReglasEventos.CabeEnPuente(x.Inicio, x.Fin, puentes))
            .Select(x => new EventoRecomendadoDto(x.Evento.Id, x.Evento.Titulo, x.Evento.Descripcion,
                x.Inicio.ToString("yyyy-MM-dd"), x.Inicio.ToString("HH:mm"), x.Fin.ToString("HH:mm"), x.Evento.Lugar, x.Evento.FuenteUrl))
            .ToList();
    }
}
