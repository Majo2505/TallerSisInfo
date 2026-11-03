using Breaku.Application.Actividades;
using Breaku.Domain;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

public class ActividadRepository : IActividadRepository
{
    private readonly AppDbContext _db;
    public ActividadRepository(AppDbContext db) => _db = db;

    public Task<LogAuditoriaRag?> ObtenerExtraccionAsync(long consultaId, int usuarioId, CancellationToken ct = default)
        => _db.LogsAuditoriaRag.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == consultaId && l.UsuarioId == usuarioId && l.Tipo == LogAuditoriaRag.TipoExtraccionComunidad, ct);

    public Task<bool> ConsultaYaUsadaAsync(long consultaId, CancellationToken ct = default)
        => _db.Comunidades.AsNoTracking().AnyAsync(c => c.LogId == consultaId, ct);

    public async Task AgregarAsync(ComunidadEfimera comunidad, CancellationToken ct = default)
    {
        _db.Comunidades.Add(comunidad);
        await _db.SaveChangesAsync(ct);
    }
}
