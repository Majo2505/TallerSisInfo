using Breaku.Application.Puentes;
using Breaku.Domain;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

public class PuenteRepository : IPuenteRepository
{
    private readonly AppDbContext _db;
    public PuenteRepository(AppDbContext db) => _db = db;

    public Task<List<Puente>> ListarPorUsuarioAsync(int usuarioId, CancellationToken ct = default)
        => _db.Puentes.AsNoTracking().Where(p => p.UsuarioId == usuarioId).ToListAsync(ct);

    public async Task ReemplazarAsync(int usuarioId, IReadOnlyList<Puente> nuevos, CancellationToken ct = default)
    {
        // Primero se borra y se confirma, y recién después se inserta: la tabla tiene
        // UNIQUE (usuario_id, dia_semana, hora_inicio) y un solo SaveChanges podría
        // insertar antes de borrar y fallar por duplicado. La transacción hace todo atómico.
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        await _db.Puentes.Where(p => p.UsuarioId == usuarioId).ExecuteDeleteAsync(ct);

        if (nuevos.Count > 0)
        {
            await _db.Puentes.AddRangeAsync(nuevos, ct);
            await _db.SaveChangesAsync(ct);
        }

        await tx.CommitAsync(ct);
    }
}