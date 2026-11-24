using Breaku.Application.Actividades;
using Breaku.Application.Notificaciones;
using Breaku.Domain;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

/// <summary>Avisos de la campanita de los administradores (tipos REPORTE_NUEVO y EVENTO_POR_REVISAR, migración 030).</summary>
public class AvisosAdminRepository : IAvisosAdminRepository
{
    private readonly AppDbContext _db;
    public AvisosAdminRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<int>> AvisarAsync(string tipo, ObjetoAvisoAdmin objeto, int objetoId, string mensaje,
        CancellationToken ct = default)
    {
        // uq_notif_reporte / uq_notif_evento (usuario, objeto, tipo): si el admin ya tenía el aviso (por ejemplo, el
        // evento volvió a revisión después de editarse), se reactiva como nuevo en vez de duplicarse.
        if (objeto == ObjetoAvisoAdmin.Reporte)
            await _db.Database.ExecuteSqlAsync($"""
                INSERT INTO notificacion (usuario_id, tipo, reporte_id, mensaje)
                SELECT u.id, {tipo}, {objetoId}, {mensaje} FROM usuario u WHERE u.rol = 'ADMIN' AND u.activo = TRUE
                ON DUPLICATE KEY UPDATE mensaje = {mensaje}, leida = FALSE, activa = TRUE, created_at = UTC_TIMESTAMP(6)
                """, ct);
        else
            await _db.Database.ExecuteSqlAsync($"""
                INSERT INTO notificacion (usuario_id, tipo, evento_id, mensaje)
                SELECT u.id, {tipo}, {objetoId}, {mensaje} FROM usuario u WHERE u.rol = 'ADMIN' AND u.activo = TRUE
                ON DUPLICATE KEY UPDATE mensaje = {mensaje}, leida = FALSE, activa = TRUE, created_at = UTC_TIMESTAMP(6)
                """, ct);

        return await _db.Database
            .SqlQuery<int>($"SELECT id AS Value FROM usuario WHERE rol = 'ADMIN' AND activo = TRUE")
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<int>> CerrarAsync(string tipo, ObjetoAvisoAdmin objeto, int objetoId, CancellationToken ct = default)
    {
        var consulta = objeto == ObjetoAvisoAdmin.Reporte
            ? _db.Notificaciones.Where(n => n.Tipo == tipo && n.ReporteId == objetoId && n.Activa && !n.Leida)
            : _db.Notificaciones.Where(n => n.Tipo == tipo && n.EventoId == objetoId && n.Activa && !n.Leida);

        var admins = await consulta.Select(n => n.UsuarioId).Distinct().ToListAsync(ct);
        if (admins.Count > 0) await consulta.ExecuteUpdateAsync(s => s.SetProperty(n => n.Leida, true), ct);
        return admins;
    }

    public async Task<CandidatoAlerta?> IntentosBloqueadosAsync(int usuarioId, DateTime desdeUtc, CancellationToken ct = default)
    {
        var usuario = await _db.Usuarios.AsNoTracking()
            .Where(u => u.Id == usuarioId && u.Activo)
            .Select(u => new { u.Id, u.Nombre, u.Rol })
            .FirstOrDefaultAsync(ct);
        if (usuario is null) return null;

        // Solo lo que escribió el estudiante (ver ReglasAlertasAdmin.CuentaComoIntento): sin modelo, o la corrección a mano.
        var manual = ExtraccionActividadService.ModeloCorreccionManual;
        var bloqueados = _db.LogsAuditoriaRag.AsNoTracking()
            .Where(l => l.UsuarioId == usuarioId && l.Estado == LogAuditoriaRag.EstadoRechazadoSeguridad
                        && (l.Modelo == null || l.Modelo == manual));
        var intentos = await bloqueados.CountAsync(l => l.CreatedAt >= desdeUtc, ct);

        // Cuántos después de su última advertencia vigente (las levantadas no cuentan), para saber si siguió tras el aviso.
        var ultimaAdvertencia = (await _db.Database.SqlQuery<DateTime>($"""
                SELECT MAX(creado_en) AS Value FROM sancion
                WHERE usuario_id = {usuarioId} AND tipo = 'ADVERTENCIA' AND levantada_en IS NULL
                HAVING MAX(creado_en) IS NOT NULL
                """).ToListAsync(ct)).Cast<DateTime?>().FirstOrDefault();
        int? desdeAdvertencia = ultimaAdvertencia is { } fecha ? await bloqueados.CountAsync(l => l.CreatedAt > fecha, ct) : null;

        return new CandidatoAlerta(usuario.Id, usuario.Nombre, usuario.Rol == "ADMIN", intentos, desdeAdvertencia);
    }

    public async Task<CandidatoAlerta?> CreadorReportadoAsync(int reporteId, CancellationToken ct = default)
    {
        var creador = (await _db.Database.SqlQuery<FilaCreador>($"""
                SELECT u.id AS Id, u.nombre AS Nombre, u.rol AS Rol
                FROM reporte r
                LEFT JOIN comunidad_efimera c ON c.id = r.comunidad_id
                LEFT JOIN evento e ON e.id = r.evento_id
                JOIN usuario u ON u.id = COALESCE(c.creador_id, e.creador_id)
                WHERE r.id = {reporteId} AND u.activo = TRUE
                """).ToListAsync(ct)).FirstOrDefault();
        if (creador is null) return null;

        // Personas distintas (sin contar al propio creador) con reportes PENDIENTES sobre sus comunidades o eventos.
        var personas = (await _db.Database.SqlQuery<long>($"""
                SELECT COUNT(DISTINCT r.usuario_id) AS Value
                FROM reporte r
                LEFT JOIN comunidad_efimera c ON c.id = r.comunidad_id
                LEFT JOIN evento e ON e.id = r.evento_id
                WHERE r.estado = 'PENDIENTE' AND r.usuario_id <> {creador.Id}
                  AND (c.creador_id = {creador.Id} OR e.creador_id = {creador.Id})
                """).ToListAsync(ct)).Single();
        return new CandidatoAlerta(creador.Id, creador.Nombre, creador.Rol == "ADMIN", (int)personas);
    }

    public async Task<IReadOnlyList<int>> AlertarAsync(string tipo, int usuarioObjetivoId, string mensaje, CancellationToken ct = default)
    {
        // uq_notif_objetivo (admin, estudiante, tipo): si el admin ya tenía la alerta, se actualiza y vuelve a quedar sin leer.
        await _db.Database.ExecuteSqlAsync($"""
                INSERT INTO notificacion (usuario_id, tipo, usuario_objetivo_id, mensaje)
                SELECT u.id, {tipo}, {usuarioObjetivoId}, {mensaje} FROM usuario u
                WHERE u.rol = 'ADMIN' AND u.activo = TRUE AND u.id <> {usuarioObjetivoId}
                ON DUPLICATE KEY UPDATE mensaje = {mensaje}, leida = FALSE, activa = TRUE, created_at = UTC_TIMESTAMP(6)
                """, ct);

        return await _db.Database
            .SqlQuery<int>($"SELECT id AS Value FROM usuario WHERE rol = 'ADMIN' AND activo = TRUE AND id <> {usuarioObjetivoId}")
            .ToListAsync(ct);
    }

    public async Task<List<FilaAlerta>> ListarAlertasActivasAsync(CancellationToken ct = default)
    {
        // Cada admin tiene su copia de la alerta: se toma la más reciente por estudiante y tipo.
        var filas = await _db.Database.SqlQuery<FilaAlertaSql>($"""
                SELECT x.UsuarioId, u.nombre AS Nombre, u.correo AS Correo, u.activo AS Activo, x.Tipo, x.Mensaje, x.Fecha
                FROM (SELECT n.usuario_objetivo_id AS UsuarioId, n.tipo AS Tipo, n.mensaje AS Mensaje, n.created_at AS Fecha,
                             ROW_NUMBER() OVER (PARTITION BY n.usuario_objetivo_id, n.tipo ORDER BY n.created_at DESC, n.id DESC) AS Orden
                      FROM notificacion n
                      WHERE n.tipo IN ('ALERTA_USUARIO_SOSPECHOSO','ALERTA_REPORTES_ACUMULADOS')
                        AND n.activa = TRUE AND n.usuario_objetivo_id IS NOT NULL) x
                JOIN usuario u ON u.id = x.UsuarioId
                WHERE x.Orden = 1
                """).ToListAsync(ct);
        return filas.Select(f => new FilaAlerta(f.UsuarioId, f.Nombre, f.Correo, f.Activo, f.Tipo, f.Mensaje, f.Fecha)).ToList();
    }

    public async Task<IReadOnlyList<int>> ResolverAlertasAsync(int usuarioObjetivoId, CancellationToken ct = default)
    {
        var alertas = _db.Notificaciones.Where(n => n.UsuarioObjetivoId == usuarioObjetivoId && n.Activa
            && (n.Tipo == Notificacion.TipoAlertaUsuarioSospechoso || n.Tipo == Notificacion.TipoAlertaReportesAcumulados));
        var admins = await alertas.Select(n => n.UsuarioId).Distinct().ToListAsync(ct);
        if (admins.Count > 0) await alertas.ExecuteUpdateAsync(s => s.SetProperty(n => n.Activa, false), ct);
        return admins;
    }

    private sealed class FilaAlertaSql
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = "";
        public string Correo { get; set; } = "";
        public bool Activo { get; set; }
        public string Tipo { get; set; } = "";
        public string Mensaje { get; set; } = "";
        public DateTime Fecha { get; set; }
    }

    private sealed class FilaCreador
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Rol { get; set; } = "";
    }
}
