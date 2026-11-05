using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Breaku.Api.Hubs;

[Authorize]
public sealed class ComunidadHub : Hub
{
    public Task Suscribirse(int comunidadId)
    {
        if (comunidadId <= 0)
            throw new HubException("El identificador de comunidad no es válido.");

        return Groups.AddToGroupAsync(Context.ConnectionId, Grupo(comunidadId));
    }

    public Task Desuscribirse(int comunidadId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, Grupo(comunidadId));

    public static string Grupo(int comunidadId) => $"comunidad:{comunidadId}";
}

public sealed record ComunidadEstadoActualizado(int ComunidadId, string Estado, int Confirmados);
