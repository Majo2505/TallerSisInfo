using Breaku.Application.Admin;
using Breaku.Domain;
using Breaku.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

/// <summary>
/// HU35: quitar y restaurar comunidades. motivo_baja y fecha_baja (migración 034) solo se leen y escriben aquí, con SQL: la
/// entidad ComunidadEfimera no las mapea, así el resto de la app no depende de esas columnas.
/// </summary>
public sealed class ModeracionComunidadRepository : IModeracionComunidadRepository
{
    private readonly AppDbContext _db;
    public ModeracionComunidadRepository(AppDbContext db) => _db = db;

    public async Task<List<FilaComunidadAdmin>> ListarAsync(string estado, DateTime ahoraUtc, int maximo, CancellationToken ct = default)
    {
        var bajadas = estado == ComunidadEfimera.EstadoBajada;
        var filas = await _db.Database.SqlQuery<FilaSql>($"""
                SELECT c.id AS Id, c.actividad AS Actividad, c.creador_id AS CreadorId, u.nombre AS Creador,
                       c.inicio AS InicioUtc, c.fin AS FinUtc, c.max_participantes AS MaxParticipantes, c.privada AS Privada,
                       c.estado AS Estado, c.motivo_baja AS MotivoBaja, c.fecha_baja AS FechaBajaUtc,
                       (SELECT COUNT(*) FROM participacion p WHERE p.comunidad_id = c.id AND p.estado = 'CONFIRMADO') AS ConfirmadosEnTabla,
                       (SELECT COUNT(*) FROM participacion p WHERE p.comunidad_id = c.id AND p.usuario_id = c.creador_id) AS CreadorEnTabla,
                       (SELECT COUNT(*) FROM reporte r WHERE r.comunidad_id = c.id AND r.estado = 'PENDIENTE') AS ReportesPendientes
                FROM comunidad_efimera c
                JOIN usuario u ON u.id = c.creador_id
                WHERE c.estado = {estado} AND ({bajadas} OR c.fin > {ahoraUtc})
                ORDER BY CASE WHEN {bajadas} THEN c.fecha_baja END DESC, c.inicio, c.id
                LIMIT {maximo}
                """).ToListAsync(ct);

        return filas.Select(f => new FilaComunidadAdmin(
            f.Id, f.Actividad, f.CreadorId, f.Creador, Utc(f.InicioUtc), Utc(f.FinUtc), (int)f.ConfirmadosEnTabla,
            f.CreadorEnTabla > 0, f.MaxParticipantes, f.Privada, f.Estado, (int)f.ReportesPendientes, f.MotivoBaja,
            f.FechaBajaUtc is { } fb ? Utc(fb) : null)).ToList();
    }

    public async Task<CambioComunidad> QuitarAsync(int comunidadId, int adminId, string motivo, Func<string, string> aviso, int? reporteId,
        string respuestaReporte, DateTime ahoraUtc, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var comunidad = await BloquearAsync(comunidadId, ct);
        if (comunidad is null) return Sin(ResultadoModeracionComunidad.NoEncontrada);
        if (comunidad.Estado != ComunidadEfimera.EstadoActiva) return Sin(ResultadoModeracionComunidad.EstadoInvalido);

        FilaReporte? reporte = null;
        if (reporteId is int idReporte)
        {
            reporte = (await _db.Database.SqlQuery<FilaReporte>($"""
                    SELECT estado AS Estado, comunidad_id AS ComunidadId, usuario_id AS UsuarioId, motivo AS Motivo
                    FROM reporte WHERE id = {idReporte} FOR UPDATE
                    """).ToListAsync(ct)).FirstOrDefault();
            if (reporte is null || reporte.ComunidadId != comunidadId) return Sin(ResultadoModeracionComunidad.ReporteNoCorresponde);
            if (reporte.Estado != ReglasReporte.Pendiente) return Sin(ResultadoModeracionComunidad.ReporteYaRevisado);
        }

        await _db.Database.ExecuteSqlAsync($"""
                UPDATE comunidad_efimera SET estado = 'BAJADA', motivo_baja = {motivo}, fecha_baja = {ahoraUtc}, updated_at = {ahoraUtc}
                WHERE id = {comunidadId}
                """, ct);

        var avisados = await AvisarAsync(comunidadId, comunidad, Notificacion.TipoComunidadBajada, aviso(comunidad.Actividad), ahoraUtc, ct);

        // Lo que ya no sirve: el aviso de restaurada de una vez anterior y las invitaciones o avisos de cupo pendientes.
        await _db.Database.ExecuteSqlAsync($"""
                UPDATE notificacion SET activa = FALSE
                WHERE comunidad_id = {comunidadId}
                  AND tipo IN ('COMUNIDAD_RESTAURADA','INVITACION_COMUNIDAD','COMUNIDAD_CERRADA','ASISTENCIA_COMUNIDAD')
                """, ct);

        if (reporte is not null)
        {
            await _db.Database.ExecuteSqlAsync($"""
                    UPDATE reporte SET estado = 'RESUELTO', revisor_id = {adminId}, revisado_en = {ahoraUtc}, respuesta = {respuestaReporte}
                    WHERE id = {reporteId} AND estado = 'PENDIENTE'
                    """, ct);
            var mensaje = ReglasReporte.MensajeAviso(reporte.Motivo, ReglasReporte.Resuelto, respuestaReporte);
            await _db.Database.ExecuteSqlAsync($"""
                    INSERT INTO notificacion (usuario_id, tipo, reporte_id, mensaje, created_at)
                    VALUES ({reporte.UsuarioId}, 'REPORTE_RESUELTO', {reporteId}, {mensaje}, {ahoraUtc})
                    ON DUPLICATE KEY UPDATE mensaje = VALUES(mensaje), leida = FALSE, activa = TRUE, created_at = VALUES(created_at)
                    """, ct);
            avisados = [.. avisados, reporte.UsuarioId];
        }

        await tx.CommitAsync(ct);
        return new CambioComunidad(ResultadoModeracionComunidad.Aplicado, avisados, 0, reporteId);
    }

    public async Task<CambioComunidad> RestaurarAsync(int comunidadId, Func<string, string> aviso, DateTime ahoraUtc, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var comunidad = await BloquearAsync(comunidadId, ct);
        if (comunidad is null) return Sin(ResultadoModeracionComunidad.NoEncontrada);
        if (comunidad.Estado != ComunidadEfimera.EstadoBajada) return Sin(ResultadoModeracionComunidad.EstadoInvalido);
        if (!ReglasModeracionComunidad.PuedeRestaurarse(Utc(comunidad.FinUtc), ahoraUtc)) return Sin(ResultadoModeracionComunidad.YaTermino);

        await _db.Database.ExecuteSqlAsync($"""
                UPDATE comunidad_efimera SET estado = 'ACTIVA', motivo_baja = NULL, fecha_baja = NULL, updated_at = {ahoraUtc}
                WHERE id = {comunidadId}
                """, ct);
        var avisados = await AvisarAsync(comunidadId, comunidad, Notificacion.TipoComunidadRestaurada, aviso(comunidad.Actividad), ahoraUtc, ct);
        await _db.Database.ExecuteSqlAsync(
            $"UPDATE notificacion SET activa = FALSE WHERE comunidad_id = {comunidadId} AND tipo = 'COMUNIDAD_BAJADA'", ct);

        var confirmados = await _db.Set<Participacion>()
            .CountAsync(p => p.ComunidadId == comunidadId && p.Estado == Participacion.EstadoConfirmado, ct);
        var creadorEnTabla = await _db.Set<Participacion>().AnyAsync(p => p.ComunidadId == comunidadId && p.UsuarioId == comunidad.CreadorId, ct);

        await tx.CommitAsync(ct);
        return new CambioComunidad(ResultadoModeracionComunidad.Aplicado, avisados,
            ReglasDescubrir.Confirmados(confirmados, creadorEnTabla), null);
    }

    /// <summary>
    /// El aviso a quien la creó y a quienes iban (CONFIRMADO). uq_notif_comunidad (usuario, comunidad, tipo): si ya lo tenían
    /// de una vez anterior, se actualiza y vuelve a quedar sin leer.
    /// </summary>
    private async Task<List<int>> AvisarAsync(int comunidadId, FilaComunidad comunidad, string tipo, string mensaje, DateTime ahoraUtc,
        CancellationToken ct)
    {
        var destinatarios = await _db.Set<Participacion>()
            .Where(p => p.ComunidadId == comunidadId && p.Estado == Participacion.EstadoConfirmado)
            .Select(p => p.UsuarioId)
            .ToListAsync(ct);
        destinatarios = destinatarios.Append(comunidad.CreadorId).Distinct().ToList();
        var activos = await _db.Usuarios.Where(u => destinatarios.Contains(u.Id) && u.Activo).Select(u => u.Id).ToListAsync(ct);

        foreach (var usuarioId in activos)
            await _db.Database.ExecuteSqlAsync($"""
                    INSERT INTO notificacion (usuario_id, tipo, comunidad_id, mensaje, created_at)
                    VALUES ({usuarioId}, {tipo}, {comunidadId}, {mensaje}, {ahoraUtc})
                    ON DUPLICATE KEY UPDATE mensaje = VALUES(mensaje), leida = FALSE, activa = TRUE, created_at = VALUES(created_at)
                    """, ct);
        return activos;
    }

    private async Task<FilaComunidad?> BloquearAsync(int comunidadId, CancellationToken ct)
        => (await _db.Database.SqlQuery<FilaComunidad>($"""
                SELECT estado AS Estado, creador_id AS CreadorId, actividad AS Actividad, fin AS FinUtc
                FROM comunidad_efimera WHERE id = {comunidadId} FOR UPDATE
                """).ToListAsync(ct)).FirstOrDefault();

    private static CambioComunidad Sin(ResultadoModeracionComunidad resultado) => new(resultado, [], 0, null);

    private static DateTime Utc(DateTime fecha) => DateTime.SpecifyKind(fecha, DateTimeKind.Utc);

    private sealed class FilaComunidad
    {
        public string Estado { get; set; } = "";
        public int CreadorId { get; set; }
        public string Actividad { get; set; } = "";
        public DateTime FinUtc { get; set; }
    }

    private sealed class FilaReporte
    {
        public string Estado { get; set; } = "";
        public int? ComunidadId { get; set; }
        public int UsuarioId { get; set; }
        public string Motivo { get; set; } = "";
    }

    private sealed class FilaSql
    {
        public int Id { get; set; }
        public string Actividad { get; set; } = "";
        public int CreadorId { get; set; }
        public string Creador { get; set; } = "";
        public DateTime InicioUtc { get; set; }
        public DateTime FinUtc { get; set; }
        public short MaxParticipantes { get; set; }
        public bool Privada { get; set; }
        public string Estado { get; set; } = "";
        public string? MotivoBaja { get; set; }
        public DateTime? FechaBajaUtc { get; set; }
        public long ConfirmadosEnTabla { get; set; }
        public long CreadorEnTabla { get; set; }
        public long ReportesPendientes { get; set; }
    }
}
