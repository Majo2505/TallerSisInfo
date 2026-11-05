using Breaku.Application.Comunidades;
using Breaku.Domain;
using Breaku.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

public sealed class ComunidadRepository : IComunidadRepository
{
    private readonly AppDbContext _db;
    public ComunidadRepository(AppDbContext db) => _db = db;

    public Task<int> ArchivarVencidasAsync(DateTime ahoraUtc, CancellationToken ct = default)
        => _db.Comunidades
            .Where(c => c.Estado == ComunidadEfimera.EstadoActiva && c.Fin <= ahoraUtc)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.Estado, ComunidadEfimera.EstadoArchivada)
                .SetProperty(c => c.UpdatedAt, ahoraUtc), ct);

    public Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default)
        => _db.Usuarios.AsNoTracking().Include(u => u.ZonaActual).FirstOrDefaultAsync(u => u.Id == usuarioId && u.Activo, ct);

    public Task<ComunidadEfimera?> ObtenerComunidadAsync(int comunidadId, CancellationToken ct = default)
        => _db.Comunidades.AsNoTracking().FirstOrDefaultAsync(c => c.Id == comunidadId, ct);

    public Task<List<int>> ListarParticipantesAsync(int comunidadId, CancellationToken ct = default)
        => _db.Set<Participacion>().Where(p => p.ComunidadId == comunidadId).Select(p => p.UsuarioId).ToListAsync(ct);

    public async Task<List<CandidatoComunidad>> ListarCandidatosAsync(ComunidadEfimera comunidad, int zonaId, ReglasMatching.Franja actividad,
        CancellationToken ct = default)
    {
        var (diaSemana, inicio, fin) = (actividad.DiaSemana, actividad.Inicio, actividad.Fin);

        var ocupados = from p in _db.Set<Participacion>()
                       join c in _db.Comunidades on p.ComunidadId equals c.Id
                       where c.Id != comunidad.Id && p.Estado == Participacion.EstadoConfirmado
                             && c.Estado == ComunidadEfimera.EstadoActiva && c.Inicio < comunidad.Fin && c.Fin > comunidad.Inicio
                       select p.UsuarioId;
        var yaDentro = _db.Set<Participacion>().Where(p => p.ComunidadId == comunidad.Id).Select(p => p.UsuarioId);

        var companeros = await _db.Usuarios.AsNoTracking()
            .Where(u => u.Id != comunidad.CreadorId && u.Activo && u.Rol == "ESTUDIANTE" && u.VisibleMatching && u.ZonaActualId == zonaId)
            .Where(u => _db.Puentes.Any(p => p.UsuarioId == u.Id && p.DiaSemana == diaSemana && p.HoraInicio < fin && p.HoraFin > inicio))
            .Where(u => !ocupados.Contains(u.Id) && !yaDentro.Contains(u.Id))
            .Select(u => new { u.Id, u.Nombre, u.Carrera, u.NotificacionesActivas })
            .ToListAsync(ct);
        if (companeros.Count == 0) return [];

        var ids = companeros.Select(c => c.Id).ToList();
        var puentes = await _db.Puentes.AsNoTracking()
            .Where(p => ids.Contains(p.UsuarioId) && p.DiaSemana == diaSemana)
            .ToListAsync(ct);
        var porUsuario = puentes.ToLookup(p => p.UsuarioId);

        return companeros
            .Select(c => new CandidatoComunidad(c.Id, c.Nombre, c.Carrera, c.NotificacionesActivas, porUsuario[c.Id].ToList()))
            .ToList();
    }

    public async Task GuardarInvitacionesAsync(IReadOnlyList<Participacion> participaciones, IReadOnlyList<Notificacion> avisos,
        CancellationToken ct = default)
    {
        if (participaciones.Count == 0 && avisos.Count == 0) return;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        _db.Set<Participacion>().AddRange(participaciones);
        _db.Notificaciones.AddRange(avisos);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
