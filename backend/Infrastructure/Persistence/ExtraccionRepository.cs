using Breaku.Application.Actividades;
using Breaku.Domain;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

public class ExtraccionRepository : IExtraccionRepository
{
    private readonly AppDbContext _db;
    public ExtraccionRepository(AppDbContext db) => _db = db;

    public async Task GuardarLogAsync(LogAuditoriaRag log, CancellationToken ct = default)
    {
        _db.LogsAuditoriaRag.Add(log);
        await _db.SaveChangesAsync(ct);
    }

    public Task<LogAuditoriaRag?> ObtenerLogAsync(long id, CancellationToken ct = default)
        => _db.LogsAuditoriaRag.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);
}
