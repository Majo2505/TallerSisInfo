using Breaku.Application.Ubicacion;
using Breaku.Domain;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

public sealed class UbicacionRepository : IUbicacionRepository
{
    private readonly AppDbContext _db;

    public UbicacionRepository(AppDbContext db) => _db = db;

    public Task<List<Zona>> ListarZonasAsync(CancellationToken ct = default)
        => _db.Zonas.AsNoTracking().ToListAsync(ct);

    public Task<Zona?> ObtenerZonaAsync(int zonaId, CancellationToken ct = default)
        => _db.Zonas.AsNoTracking().FirstOrDefaultAsync(z => z.Id == zonaId, ct);

    public Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default)
        => _db.Usuarios.Include(u => u.ZonaActual)
            .FirstOrDefaultAsync(u => u.Id == usuarioId && u.Activo, ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
