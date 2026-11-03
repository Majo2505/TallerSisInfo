using Breaku.Application.Comunidades;
using Breaku.Domain;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

public sealed class DescubrirComunidadesRepository : IDescubrirComunidadesRepository
{
    private readonly AppDbContext _db;
    public DescubrirComunidadesRepository(AppDbContext db) => _db = db;

    public Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default)
        => _db.Usuarios.AsNoTracking().Include(u => u.ZonaActual).FirstOrDefaultAsync(u => u.Id == usuarioId && u.Activo, ct);

    public Task<List<ComunidadActiva>> ListarActivasAsync(int usuarioId, int? deOtrosEnZona, DateTime ahoraUtc, DateTime hastaUtc,
        int maximo, CancellationToken ct = default)
    {
        var participaciones = _db.Set<Participacion>();

        // Usa ix_comunidad_estado_fin. Las de otros: misma sede y consentimiento del matching (HU12), como las invitaciones de HU13.
        return (from c in _db.Comunidades.AsNoTracking()
                join u in _db.Usuarios on c.CreadorId equals u.Id
                where c.Estado == ComunidadEfimera.EstadoActiva && c.Fin > ahoraUtc && c.Inicio < hastaUtc
                      && (c.CreadorId == usuarioId
                          || participaciones.Any(p => p.ComunidadId == c.Id && p.UsuarioId == usuarioId)
                          || (deOtrosEnZona != null && u.Activo && u.VisibleMatching && u.ZonaActualId == deOtrosEnZona))
                orderby c.Inicio, c.Id
                select new ComunidadActiva(
                    c.Id, c.CreadorId, u.Nombre, c.Actividad, c.Inicio, c.Fin, c.MaxParticipantes,
                    participaciones.Count(p => p.ComunidadId == c.Id && p.Estado == Participacion.EstadoConfirmado),
                    participaciones.Any(p => p.ComunidadId == c.Id && p.UsuarioId == c.CreadorId),
                    participaciones.Where(p => p.ComunidadId == c.Id && p.UsuarioId == usuarioId).Select(p => p.Estado).FirstOrDefault()))
            .Take(maximo)
            .ToListAsync(ct);
    }
}
