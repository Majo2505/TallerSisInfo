using Breaku.Application.Perfil;
using Breaku.Domain;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

public sealed class PerfilRepository : IPerfilRepository
{
    private readonly AppDbContext _db;
    public PerfilRepository(AppDbContext db) => _db = db;

    public Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default)
        => _db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId && u.Activo, ct);

    public async Task GuardarConHistorialAsync(int usuarioId, string valoresAnteriores, string valoresNuevos, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        await _db.SaveChangesAsync(ct);
        // historial_cambio no está mapeado (lo comparten varias historias): se inserta con SQL.
        await _db.Database.ExecuteSqlAsync($"""
                INSERT INTO historial_cambio (entidad, entidad_id, accion, valores_anteriores, valores_nuevos, usuario_id)
                VALUES ('USUARIO', {usuarioId}, 'UPDATE', CAST({valoresAnteriores} AS JSON), CAST({valoresNuevos} AS JSON), {usuarioId})
                """, ct);
        await tx.CommitAsync(ct);
    }
}
