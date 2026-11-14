using Breaku.Application.Lugares;
using Breaku.Domain;
using Breaku.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

public class LugarRepository : ILugarRepository
{
    private readonly AppDbContext _db;
    public LugarRepository(AppDbContext db) => _db = db;

    public Task<Zona?> ObtenerZonaDelUsuarioAsync(int usuarioId, CancellationToken ct = default)
        => _db.Usuarios.AsNoTracking()
            .Where(u => u.Id == usuarioId && u.ZonaActualId != null && u.ZonaActual!.Activa)
            .Select(u => u.ZonaActual)
            .FirstOrDefaultAsync(ct);

    public Task<List<Lugar>> ListarActivosAsync(CancellationToken ct = default)
        => _db.Lugares.AsNoTracking()
            .Include(l => l.Categoria)
            .Where(l => l.Estado == ReglasBusquedaLugares.EstadoActivo && l.Recomendable)
            .ToListAsync(ct);

    public async Task<Dictionary<int, (int MeGusta, int NoMeSirve)>> ContarVotosAsync(IReadOnlyCollection<int> lugarIds, CancellationToken ct = default)
    {
        if (lugarIds.Count == 0) return [];
        var votos = await _db.Recomendaciones.AsNoTracking()
            .Where(r => lugarIds.Contains(r.LugarId) && r.Feedback != null)
            .GroupBy(r => r.LugarId)
            .Select(g => new
            {
                LugarId = g.Key,
                MeGusta = g.Count(r => r.Feedback == ReglasFeedback.MeGusta),
                NoMeSirve = g.Count(r => r.Feedback == ReglasFeedback.NoMeSirve),
            })
            .ToListAsync(ct);
        return votos.ToDictionary(v => v.LugarId, v => (v.MeGusta, v.NoMeSirve));
    }
}
