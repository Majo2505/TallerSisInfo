using Breaku.Application.Recomendaciones;
using Breaku.Domain;

namespace Breaku.Infrastructure.Persistence;

public class RecomendacionRepository : IRecomendacionRepository
{
    private readonly AppDbContext _db;
    public RecomendacionRepository(AppDbContext db) => _db = db;

    public async Task GuardarAsync(LogAuditoriaRag log, IReadOnlyList<Recomendacion> recomendaciones, CancellationToken ct = default)
    {
        // Un solo SaveChanges = una transacción. EF inserta en orden log → lugar_recuperado → recomendacion
        // y propaga el id del log por las navegaciones.
        _db.LogsAuditoriaRag.Add(log);
        _db.Recomendaciones.AddRange(recomendaciones);
        await _db.SaveChangesAsync(ct);
    }
}
