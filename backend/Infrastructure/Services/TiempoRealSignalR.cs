using Breaku.Api.Hubs;
using Breaku.Application.TiempoReal;
using Breaku.Domain;
using Breaku.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Breaku.Infrastructure.Services;

/// <summary>HU18: los avisos en vivo por SignalR (<c>/hubs/comunidades</c>). Un fallo acá nunca rompe la operación que lo pidió.</summary>
public sealed class TiempoRealSignalR : ITiempoReal
{
    public const string EventoAvisos = "avisosCambiaron";
    public const string EventoComunidades = "comunidadesCambiaron";

    private readonly IHubContext<ComunidadHub> _hub;
    private readonly AppDbContext _db;
    private readonly ILogger<TiempoRealSignalR> _logger;

    public TiempoRealSignalR(IHubContext<ComunidadHub> hub, AppDbContext db, ILogger<TiempoRealSignalR> logger)
    {
        _hub = hub;
        _db = db;
        _logger = logger;
    }

    public async Task AvisosCambiaronAsync(IReadOnlyCollection<int> usuarioIds, CancellationToken ct = default)
    {
        if (usuarioIds.Count == 0) return;
        try
        {
            var ids = usuarioIds.Distinct().Select(id => id.ToString()).ToList();
            await _hub.Clients.Users(ids).SendAsync(EventoAvisos, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "No se pudo avisar en vivo a {Cantidad} estudiantes.", usuarioIds.Count);
        }
    }

    public async Task ComunidadCreadaAsync(int comunidadId, CancellationToken ct = default)
    {
        try
        {
            var zona = await (from c in _db.Comunidades
                              join u in _db.Usuarios on c.CreadorId equals u.Id
                              where c.Id == comunidadId
                              select u.ZonaActualId).FirstOrDefaultAsync(ct);
            if (zona is int zonaId)
                await _hub.Clients.Group(ComunidadHub.GrupoZona(zonaId)).SendAsync(EventoComunidades, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "No se pudo avisar en vivo la comunidad nueva {ComunidadId}.", comunidadId);
        }

        await AvisosDeComunidadAsync(comunidadId, ct);
    }

    public async Task AvisosDeComunidadAsync(int comunidadId, CancellationToken ct = default)
    {
        List<int> participantes;
        try
        {
            participantes = await _db.Set<Participacion>().AsNoTracking()
                .Where(p => p.ComunidadId == comunidadId).Select(p => p.UsuarioId).ToListAsync(ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "No se pudo avisar en vivo a los de la comunidad {ComunidadId}.", comunidadId);
            return;
        }

        await AvisosCambiaronAsync(participantes, ct);
    }
}
