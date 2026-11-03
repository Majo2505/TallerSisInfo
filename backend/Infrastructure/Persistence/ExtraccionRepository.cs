using Breaku.Application.Actividades;
using Breaku.Domain;

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
}
