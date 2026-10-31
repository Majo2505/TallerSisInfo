using Breaku.Application.Recomendaciones;
using Breaku.Domain;
using Microsoft.EntityFrameworkCore;

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

    public Task<Recomendacion?> ObtenerDeUsuarioAsync(long recomendacionId, int usuarioId, CancellationToken ct = default) =>
        _db.Recomendaciones
            .Include(r => r.LugarRecuperado)
            .ThenInclude(r => r.Log)
            .Where(r => r.Id == recomendacionId && r.LugarRecuperado.Log.UsuarioId == usuarioId)
            .SingleOrDefaultAsync(ct);

    public async Task GuardarFeedbackAsync(Recomendacion recomendacion, string feedback, CancellationToken ct = default)
    {
        recomendacion.Feedback = feedback;
        recomendacion.FechaFeedback = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
