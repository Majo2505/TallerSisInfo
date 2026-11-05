using System.Security.Claims;
using Breaku.Application.Comunidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Breaku.Api.Hubs;

[Authorize]
public sealed class ComunidadHub : Hub
{
    private readonly DescubrirComunidadesService _descubrir;

    public ComunidadHub(DescubrirComunidadesService descubrir) => _descubrir = descubrir;

    public async Task Suscribirse(int comunidadId)
    {
        if (comunidadId <= 0)
            throw new HubException("El identificador de comunidad no es válido.");

        var claim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("sub");
        if (!int.TryParse(claim, out var usuarioId) || !await _descubrir.PuedeVerAsync(usuarioId, comunidadId, Context.ConnectionAborted))
            throw new HubException("No puedes seguir esta comunidad.");

        await Groups.AddToGroupAsync(Context.ConnectionId, Grupo(comunidadId));
    }

    public Task Desuscribirse(int comunidadId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, Grupo(comunidadId));

    public static string Grupo(int comunidadId) => $"comunidad:{comunidadId}";
}

public sealed record ComunidadEstadoActualizado(int ComunidadId, string Estado, int Confirmados);
