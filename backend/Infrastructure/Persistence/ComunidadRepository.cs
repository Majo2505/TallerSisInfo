using Breaku.Application.Comunidades;
using Breaku.Domain;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

public sealed class ComunidadRepository(AppDbContext db) : IComunidadRepository
{
    public Task<ComunidadEfimera?> ObtenerAsync(int id, CancellationToken ct = default)
        => db.Comunidades.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<List<ComunidadEfimera>> ListarActivasAsync(DateTime ahora, CancellationToken ct = default)
        => db.Comunidades.AsNoTracking().Where(c => c.Estado == "ACTIVA" && c.Fin > ahora)
            .OrderBy(c => c.Inicio).Take(100).ToListAsync(ct);

    public Task<int> ContarParticipantesAsync(int comunidadId, CancellationToken ct = default)
        => db.Participaciones.CountAsync(p => p.ComunidadId == comunidadId && p.Estado == "CONFIRMADO", ct);

    public async Task<List<int>> ListarCandidatosAsync(int creadorId, DateTime inicio, DateTime fin, int maximo, CancellationToken ct = default)
    {
        var creador = await db.Usuarios.AsNoTracking().Where(u => u.Id == creadorId)
            .Select(u => new { u.ZonaActualId }).FirstOrDefaultAsync(ct);
        if (creador?.ZonaActualId is not int zonaId) return [];
        var dia = (int)inicio.DayOfWeek;
        if (dia is 0 or 7) return [];
        var horaInicio = TimeOnly.FromDateTime(inicio);
        var horaFin = TimeOnly.FromDateTime(fin);
        return await db.Usuarios.AsNoTracking()
            .Where(u => u.Id != creadorId && u.Activo && u.Rol == "ESTUDIANTE" && u.VisibleMatching && u.ZonaActualId == zonaId)
            .Where(u => db.Puentes.Any(p => p.UsuarioId == u.Id && p.DiaSemana == dia
                && p.HoraInicio < horaFin && p.HoraFin > horaInicio))
            .Select(u => u.Id).Take(Math.Max(0, maximo - 1)).ToListAsync(ct);
    }

    public async Task<ComunidadEfimera> CrearAsync(ComunidadEfimera comunidad, IReadOnlyCollection<int> usuarios, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        db.Comunidades.Add(comunidad);
        await db.SaveChangesAsync(ct);
        foreach (var usuarioId in usuarios.Distinct())
            db.Participaciones.Add(new Participacion { ComunidadId = comunidad.Id, UsuarioId = usuarioId, Estado = usuarioId == comunidad.CreadorId ? "CONFIRMADO" : "PENDIENTE", InvitadoEn = DateTime.UtcNow });
        foreach (var usuarioId in usuarios.Where(id => id != comunidad.CreadorId).Distinct())
            db.Notificaciones.Add(new Notificacion
            {
                UsuarioId = usuarioId, Tipo = "INVITACION_COMUNIDAD", ComunidadId = comunidad.Id,
                Mensaje = $"Te invitaron a la comunidad «{comunidad.Actividad}».", CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return comunidad;
    }

    public async Task<bool> ResponderAsync(int comunidadId, int usuarioId, string estado, CancellationToken ct = default)
        => await db.Participaciones.Where(p => p.ComunidadId == comunidadId && p.UsuarioId == usuarioId && p.Estado == "PENDIENTE")
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Estado, estado).SetProperty(p => p.RespondidoEn, DateTime.UtcNow), ct) > 0;

    public Task ArchivarVencidasAsync(DateTime ahora, CancellationToken ct = default)
        => db.Comunidades.Where(c => c.Estado == "ACTIVA" && c.Fin <= ahora)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Estado, "ARCHIVADA").SetProperty(c => c.UpdatedAt, ahora), ct);
}
