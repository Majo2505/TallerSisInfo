using Breaku.Api.Hubs;
using Breaku.Application.TiempoReal;
using Microsoft.AspNetCore.SignalR;

namespace Breaku.Presentation.TiempoReal;

/// <summary>HU18/HU20: el canal en vivo por SignalR (<c>/hubs/comunidades</c>). Solo envía; los destinatarios los decide Application.</summary>
public sealed class CanalSignalR : ICanalEnVivo
{
    private readonly IHubContext<ComunidadHub> _hub;

    public CanalSignalR(IHubContext<ComunidadHub> hub) => _hub = hub;

    public Task AvisosCambiaronAsync(IReadOnlyCollection<int> usuarioIds, CancellationToken ct = default)
        => _hub.Clients.Users(usuarioIds.Select(id => id.ToString()).ToList()).SendAsync(ComunidadHub.EventoAvisos, ct);

    public Task ComunidadesCambiaronAsync(int zonaId, CancellationToken ct = default)
        => _hub.Clients.Group(ComunidadHub.GrupoZona(zonaId)).SendAsync(ComunidadHub.EventoComunidades, ct);

    public Task ComunidadActualizadaAsync(int comunidadId, string estado, int confirmados, CancellationToken ct = default)
        => _hub.Clients.Group(ComunidadHub.Grupo(comunidadId)).SendAsync(
            ComunidadHub.EventoComunidadActualizada, new ComunidadEstadoActualizado(comunidadId, estado, confirmados), ct);
}
