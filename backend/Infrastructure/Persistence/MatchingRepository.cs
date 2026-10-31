using Breaku.Application.Matching;
using Breaku.Domain;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

public sealed class MatchingRepository : IMatchingRepository
{
    private readonly AppDbContext _db;
    public MatchingRepository(AppDbContext db) => _db = db;

    public Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default)
        => _db.Usuarios.Include(u => u.ZonaActual).FirstOrDefaultAsync(u => u.Id == usuarioId && u.Activo, ct);

    public async Task<List<CompaneroCandidato>> ListarCandidatosAsync(int usuarioId, int zonaId, Puente puente, CancellationToken ct = default)
    {
        // Cruce por franja: mismo día y horas que se solapan. Usa ix_puente_matching (dia_semana, hora_inicio, hora_fin).
        // El mínimo de 30 minutos en común lo aplica ReglasMatching.
        var companeros = await _db.Usuarios.AsNoTracking()
            .Where(u => u.Id != usuarioId && u.Activo && u.Rol == "ESTUDIANTE" && u.VisibleMatching && u.ZonaActualId == zonaId)
            .Where(u => _db.Puentes.Any(p => p.UsuarioId == u.Id && p.DiaSemana == puente.DiaSemana
                                             && p.HoraInicio < puente.HoraFin && p.HoraFin > puente.HoraInicio))
            .Select(u => new { u.Id, u.Nombre, u.Carrera, u.Semestre })
            .ToListAsync(ct);
        if (companeros.Count == 0) return [];

        // Toda su semana, para contar en cuántas franjas coinciden.
        var ids = companeros.Select(c => c.Id).ToList();
        var puentes = await _db.Puentes.AsNoTracking().Where(p => ids.Contains(p.UsuarioId)).ToListAsync(ct);
        var porUsuario = puentes.ToLookup(p => p.UsuarioId);

        return companeros
            .Select(c => new CompaneroCandidato(c.Id, c.Nombre, c.Carrera, c.Semestre, porUsuario[c.Id].ToList()))
            .ToList();
    }

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
