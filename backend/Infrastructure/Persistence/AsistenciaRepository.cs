using Breaku.Application.Comunidades;
using Breaku.Domain;
using Breaku.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

public sealed class AsistenciaRepository : IAsistenciaRepository
{
    private readonly AppDbContext _db;
    public AsistenciaRepository(AppDbContext db) => _db = db;

    public async Task ResponderAsync(int comunidadId, int usuarioId, DateTime ahoraUtc, Func<ContextoAsistencia, CambioAsistencia> decidir,
        CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var bloqueada = await _db.Database
            .SqlQuery<int>($"SELECT id AS Value FROM comunidad_efimera WHERE id = {comunidadId} FOR UPDATE")
            .ToListAsync(ct);
        var comunidad = bloqueada.Count == 0
            ? null
            : await _db.Comunidades.AsNoTracking().FirstOrDefaultAsync(c => c.Id == comunidadId, ct);
        if (comunidad is null)
        {
            decidir(new ContextoAsistencia(null, null, false, 0, null, []));
            return;
        }

        var participaciones = _db.Set<Participacion>();
        var mia = await participaciones.FirstOrDefaultAsync(p => p.ComunidadId == comunidadId && p.UsuarioId == usuarioId, ct);
        var confirmadosEnTabla = await participaciones
            .CountAsync(p => p.ComunidadId == comunidadId && p.Estado == Participacion.EstadoConfirmado, ct);
        var creadorEnTabla = await participaciones
            .AnyAsync(p => p.ComunidadId == comunidadId && p.UsuarioId == comunidad.CreadorId, ct);

        var puedeVerla = mia is null && await PuedeVerlaAsync(comunidad.CreadorId, usuarioId, ct);

        var cruce = await (from c in _db.Comunidades.AsNoTracking()
                           where c.Id != comunidadId && c.Estado == ComunidadEfimera.EstadoActiva
                                 && c.Inicio < comunidad.Fin && c.Fin > comunidad.Inicio
                                 && (c.CreadorId == usuarioId
                                     || participaciones.Any(p => p.ComunidadId == c.Id && p.UsuarioId == usuarioId
                                                                 && p.Estado == Participacion.EstadoConfirmado))
                           orderby c.Inicio, c.Id
                           select new CruceAsistencia(c.Actividad, c.Inicio, c.Fin))
            .FirstOrDefaultAsync(ct);

        var pendientesConAviso = await (from p in participaciones
                                        join u in _db.Usuarios on p.UsuarioId equals u.Id
                                        where p.ComunidadId == comunidadId && p.Estado == Participacion.EstadoPendiente
                                              && p.UsuarioId != usuarioId && u.Activo && u.NotificacionesActivas
                                        select p.UsuarioId)
            .ToListAsync(ct);

        var cambio = decidir(new ContextoAsistencia(
            comunidad, mia?.Estado, puedeVerla,
            ReglasDescubrir.Confirmados(confirmadosEnTabla, creadorEnTabla),
            cruce, pendientesConAviso));

        if (cambio.Guardar)
        {
            if (mia is null)
                participaciones.Add(new Participacion
                {
                    ComunidadId = comunidadId, UsuarioId = usuarioId, Estado = cambio.Estado, InvitadoEn = ahoraUtc, RespondidoEn = ahoraUtc
                });
            else
            {
                mia.Estado = cambio.Estado;
                mia.RespondidoEn = ahoraUtc;
            }
            await _db.SaveChangesAsync(ct);
        }

        await _db.Notificaciones
            .Where(n => n.UsuarioId == usuarioId && n.ComunidadId == comunidadId && !n.Leida)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Leida, true), ct);

        if (cambio.Reabrir)
            await _db.Notificaciones
                .Where(n => n.ComunidadId == comunidadId && n.Tipo == Notificacion.TipoComunidadCerrada && n.Activa)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.Activa, false), ct);

        foreach (var invitado in cambio.AvisarCerrada)
            await _db.Database.ExecuteSqlAsync(
                $"""
                 INSERT INTO notificacion (usuario_id, tipo, comunidad_id, mensaje, leida, activa, created_at)
                 VALUES ({invitado}, {Notificacion.TipoComunidadCerrada}, {comunidadId}, {cambio.MensajeCerrada}, FALSE, TRUE, {ahoraUtc})
                 ON DUPLICATE KEY UPDATE mensaje = VALUES(mensaje), leida = FALSE, activa = TRUE, created_at = VALUES(created_at)
                 """, ct);

        await tx.CommitAsync(ct);
    }

    public async Task<ParticipantesComunidad?> ObtenerParticipantesAsync(int comunidadId, CancellationToken ct = default)
    {
        var comunidad = await _db.Comunidades.AsNoTracking().FirstOrDefaultAsync(c => c.Id == comunidadId, ct);
        if (comunidad is null) return null;

        var nombreCreador = await _db.Usuarios.AsNoTracking()
            .Where(u => u.Id == comunidad.CreadorId)
            .Select(u => u.Nombre)
            .FirstOrDefaultAsync(ct) ?? "";

        var filas = await (from p in _db.Set<Participacion>().AsNoTracking()
                           join u in _db.Usuarios on p.UsuarioId equals u.Id
                           where p.ComunidadId == comunidadId
                           select new FilaParticipante(p.UsuarioId, u.Nombre, u.Carrera, p.Estado, p.RespondidoEn))
            .ToListAsync(ct);

        return new ParticipantesComunidad(comunidad, nombreCreador, filas);
    }

    private async Task<bool> PuedeVerlaAsync(int creadorId, int usuarioId, CancellationToken ct)
    {
        var yo = await _db.Usuarios.AsNoTracking()
            .Where(u => u.Id == usuarioId && u.Activo)
            .Select(u => new { u.ZonaActualId, ZonaActiva = u.ZonaActual != null && u.ZonaActual.Activa, u.VisibleMatching })
            .FirstOrDefaultAsync(ct);
        if (yo is null || yo.ZonaActualId is null || !yo.ZonaActiva || !yo.VisibleMatching) return false;

        return await _db.Usuarios.AnyAsync(
            u => u.Id == creadorId && u.Activo && u.VisibleMatching && u.ZonaActualId == yo.ZonaActualId, ct);
    }
}
