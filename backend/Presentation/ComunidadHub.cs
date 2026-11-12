using System.Security.Claims;
using Breaku.Application.Comunidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Breaku.Api.Hubs;

/// <summary>
/// HU18: tiempo real de la app. Eventos que manda el servidor:
/// <c>comunidadActualizada</c> (a quienes siguen esa comunidad: cupo o archivo),
/// <c>comunidadesCambiaron</c> (a quienes miran Descubrir en esa sede: hay una comunidad nueva) y
/// <c>avisosCambiaron</c> (a un estudiante: su campanita tiene algo nuevo).
/// </summary>
[Authorize]
public sealed class ComunidadHub : Hub
{
    private readonly DescubrirComunidadesService _descubrir;

    public ComunidadHub(DescubrirComunidadesService descubrir) => _descubrir = descubrir;

    public async Task Suscribirse(int comunidadId)
    {
        if (comunidadId <= 0)
            throw new HubException("El identificador de comunidad no es válido.");

        if (UsuarioId() is not int usuarioId || !await _descubrir.PuedeVerAsync(usuarioId, comunidadId, Context.ConnectionAborted))
            throw new HubException("No puedes seguir esta comunidad.");

        await Groups.AddToGroupAsync(Context.ConnectionId, Grupo(comunidadId));
    }

    public Task Desuscribirse(int comunidadId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, Grupo(comunidadId));

    /// <summary>Descubrir abierto: recibe <c>comunidadesCambiaron</c> cuando alguien de tu sede crea una comunidad.</summary>
    public async Task SeguirDescubrir()
    {
        if (UsuarioId() is not int usuarioId) throw new HubException("Sesión no válida.");
        if (await _descubrir.ZonaParaSeguirAsync(usuarioId, Context.ConnectionAborted) is int zonaId)
            await Groups.AddToGroupAsync(Context.ConnectionId, GrupoZona(zonaId));
    }

    private int? UsuarioId() => UsuarioIdProvider.Leer(Context.User);

    public static string Grupo(int comunidadId) => $"comunidad:{comunidadId}";

    public static string GrupoZona(int zonaId) => $"zona:{zonaId}";
}

/// <summary>Para <c>Clients.User(id)</c>: el id del estudiante sale del token (<c>sub</c>), como en los controladores.</summary>
public sealed class UsuarioIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) => Leer(connection.User)?.ToString();

    public static int? Leer(ClaimsPrincipal? usuario)
    {
        var claim = usuario?.FindFirstValue(ClaimTypes.NameIdentifier) ?? usuario?.FindFirstValue("sub");
        return int.TryParse(claim, out var id) ? id : null;
    }
}

public sealed record ComunidadEstadoActualizado(int ComunidadId, string Estado, int Confirmados);
