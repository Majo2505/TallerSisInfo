using Breaku.Application.Sanciones;
using Breaku.Domain;
using Breaku.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

/// <summary>
/// HU28/HU29: cada paso de la purga como SQL explícito. Ninguna clave foránea de estas tablas lleva ON DELETE CASCADE
/// (salvo <c>evento_imagen</c>), así que cada método borra primero lo que apunta a lo que va a borrar.
/// El orden entre métodos lo fija <see cref="ReglasPurga.Pasos"/> y lo ejecuta <see cref="PurgaUsuarioService"/>.
/// </summary>
public sealed class PurgaRepository : IPurgaRepository
{
    private readonly AppDbContext _db;
    public PurgaRepository(AppDbContext db) => _db = db;

    public async Task<List<ReglasPurga.ComunidadCreada>> ListarComunidadesCreadasAsync(int usuarioId, CancellationToken ct = default)
    {
        var comunidades = await _db.Comunidades.AsNoTracking().Where(c => c.CreadorId == usuarioId)
            .Select(c => new { c.Id, c.Estado }).ToListAsync(ct);
        if (comunidades.Count == 0) return [];

        var ids = comunidades.Select(c => c.Id).ToList();
        var participaciones = (await _db.Set<Participacion>().AsNoTracking().Where(p => ids.Contains(p.ComunidadId)).ToListAsync(ct))
            .ToLookup(p => p.ComunidadId);

        return comunidades
            .Select(c => new ReglasPurga.ComunidadCreada(c.Id, c.Estado,
                participaciones[c.Id].Select(p => new ReglasPurga.ParticipanteComunidad(p.UsuarioId, p.Estado, p.RespondidoEn)).ToList()))
            .ToList();
    }

    public Task CambiarCreadorAsync(int comunidadId, int nuevoCreadorId, DateTime ahoraUtc, CancellationToken ct = default)
        // log_id en NULL antes de borrar los logs; texto_original es NOT NULL sin CHECK: se guarda vacío.
        => _db.Database.ExecuteSqlAsync($"""
                UPDATE comunidad_efimera
                SET creador_id = {nuevoCreadorId}, log_id = NULL, texto_original = '', updated_at = {ahoraUtc}
                WHERE id = {comunidadId}
                """, ct);

    public async Task EliminarComunidadAsync(int comunidadId, CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlAsync(
            $"DELETE FROM notificacion WHERE reporte_id IN (SELECT id FROM reporte WHERE comunidad_id = {comunidadId})", ct);
        await _db.Database.ExecuteSqlAsync($"DELETE FROM notificacion WHERE comunidad_id = {comunidadId}", ct);
        await _db.Database.ExecuteSqlAsync($"DELETE FROM reporte WHERE comunidad_id = {comunidadId}", ct);
        await _db.Database.ExecuteSqlAsync($"DELETE FROM participacion WHERE comunidad_id = {comunidadId}", ct);
        await _db.Database.ExecuteSqlAsync($"DELETE FROM comunidad_efimera WHERE id = {comunidadId}", ct);
    }

    public async Task<List<(int ComunidadId, int Confirmados)>> EliminarParticipacionesAsync(int usuarioId, CancellationToken ct = default)
    {
        var activas = await (from p in _db.Set<Participacion>().AsNoTracking()
                             join c in _db.Comunidades.AsNoTracking() on p.ComunidadId equals c.Id
                             where p.UsuarioId == usuarioId && p.Estado == Participacion.EstadoConfirmado
                                   && c.Estado == ComunidadEfimera.EstadoActiva
                             select c.Id).ToListAsync(ct);

        await _db.Database.ExecuteSqlAsync($"DELETE FROM participacion WHERE usuario_id = {usuarioId}", ct);

        var resultado = new List<(int, int)>();
        foreach (var id in activas)
            resultado.Add((id, await _db.Set<Participacion>().CountAsync(p => p.ComunidadId == id && p.Estado == Participacion.EstadoConfirmado, ct)));
        return resultado;
    }

    public Task EliminarNotificacionesAsync(int usuarioId, CancellationToken ct = default)
        => _db.Database.ExecuteSqlAsync($"DELETE FROM notificacion WHERE usuario_id = {usuarioId}", ct);

    public async Task EliminarConexionesAsync(int usuarioId, CancellationToken ct = default)
    {
        // fk_notif_conexion no lleva ON DELETE: primero los avisos de OTROS usuarios que apuntan a sus conexiones.
        await _db.Database.ExecuteSqlAsync($"""
                DELETE FROM notificacion WHERE conexion_id IN (
                  SELECT id FROM conexion WHERE solicitante_id = {usuarioId} OR destinatario_id = {usuarioId})
                """, ct);
        await _db.Database.ExecuteSqlAsync(
            $"DELETE FROM conexion WHERE solicitante_id = {usuarioId} OR destinatario_id = {usuarioId}", ct);
        await _db.Database.ExecuteSqlAsync($"DELETE FROM test_afinidad WHERE usuario_id = {usuarioId}", ct);
    }

    public async Task EliminarEventosAsync(int usuarioId, CancellationToken ct = default)
    {
        // fk_notif_evento y fk_rep_evento no llevan ON DELETE: primero lo de OTROS usuarios que apunta a estos eventos.
        await _db.Database.ExecuteSqlAsync($"""
                DELETE FROM notificacion WHERE reporte_id IN (
                  SELECT r.id FROM reporte r JOIN evento e ON e.id = r.evento_id WHERE e.creador_id = {usuarioId})
                """, ct);
        await _db.Database.ExecuteSqlAsync(
            $"DELETE FROM notificacion WHERE evento_id IN (SELECT id FROM evento WHERE creador_id = {usuarioId})", ct);
        await _db.Database.ExecuteSqlAsync(
            $"DELETE FROM reporte WHERE evento_id IN (SELECT id FROM evento WHERE creador_id = {usuarioId})", ct);
        await _db.Database.ExecuteSqlAsync(
            $"DELETE FROM historial_cambio WHERE entidad = 'EVENTO' AND entidad_id IN (SELECT id FROM evento WHERE creador_id = {usuarioId})", ct);
        // evento_imagen se borra por cascada con el evento.
        await _db.Database.ExecuteSqlAsync($"DELETE FROM evento WHERE creador_id = {usuarioId}", ct);
    }

    public async Task EliminarReportesAsync(int usuarioId, CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlAsync(
            $"DELETE FROM notificacion WHERE reporte_id IN (SELECT id FROM reporte WHERE usuario_id = {usuarioId})", ct);
        // sancion.reporte_id es ON DELETE SET NULL: la sanción conserva su motivo.
        await _db.Database.ExecuteSqlAsync($"DELETE FROM reporte WHERE usuario_id = {usuarioId}", ct);
    }

    public Task EliminarRecomendacionesAsync(int usuarioId, CancellationToken ct = default)
        => _db.Database.ExecuteSqlAsync(
            $"DELETE FROM recomendacion WHERE log_id IN (SELECT id FROM log_auditoria_rag WHERE usuario_id = {usuarioId})", ct);

    public Task EliminarLugaresRecuperadosAsync(int usuarioId, CancellationToken ct = default)
        => _db.Database.ExecuteSqlAsync(
            $"DELETE FROM lugar_recuperado WHERE log_id IN (SELECT id FROM log_auditoria_rag WHERE usuario_id = {usuarioId})", ct);

    public Task EliminarLogsAuditoriaAsync(int usuarioId, CancellationToken ct = default)
        => _db.Database.ExecuteSqlAsync($"DELETE FROM log_auditoria_rag WHERE usuario_id = {usuarioId}", ct);

    public Task EliminarPuentesAsync(int usuarioId, CancellationToken ct = default)
        => _db.Database.ExecuteSqlAsync($"DELETE FROM puente WHERE usuario_id = {usuarioId}", ct);

    public Task EliminarHorariosAsync(int usuarioId, CancellationToken ct = default)
        => _db.Database.ExecuteSqlAsync($"DELETE FROM horario WHERE usuario_id = {usuarioId}", ct);

    public Task EliminarHistorialAsync(int usuarioId, CancellationToken ct = default)
        => _db.Database.ExecuteSqlAsync(
            $"DELETE FROM historial_cambio WHERE usuario_id = {usuarioId} OR (entidad = 'USUARIO' AND entidad_id = {usuarioId})", ct);

    public Task EliminarSancionesAsync(int usuarioId, CancellationToken ct = default)
        => _db.Database.ExecuteSqlAsync($"DELETE FROM sancion WHERE usuario_id = {usuarioId}", ct);

    public Task AnonimizarUsuarioAsync(int usuarioId, string hashInutilizable, DateTime ahoraUtc, CancellationToken ct = default)
        // Se conservan correo, google_sub, rol y created_at. ck_usuario_credencial exige password_hash o google_sub:
        // el hash inutilizable cubre las cuentas que solo entraban con contraseña.
        => _db.Database.ExecuteSqlAsync($"""
                UPDATE usuario
                SET nombre = {ReglasSancion.NombreCuentaDeBaja}, carrera = NULL, semestre = NULL,
                    password_hash = {hashInutilizable}, correo_verificado = 0,
                    consentimiento_ubicacion = 0, fecha_consentimiento = NULL,
                    notificaciones_activas = 0, visible_matching = 0, fecha_visible_matching = NULL,
                    zona_actual_id = NULL, updated_by = NULL, activo = 0, updated_at = {ahoraUtc}
                WHERE id = {usuarioId}
                """, ct);

    public Task EliminarUsuarioAsync(int usuarioId, CancellationToken ct = default)
        => _db.Database.ExecuteSqlAsync($"DELETE FROM usuario WHERE id = {usuarioId}", ct);
}
