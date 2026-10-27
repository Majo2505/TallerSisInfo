using Breaku.Application.Admin;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

/// <summary>
/// Panel de administración. Lee el catálogo, los reportes y el log del RAG con SQL, sin mapear Lugar,
/// Categoria ni Reporte, para no chocar con las entidades que agregan otras historias (HU6 en adelante).
/// </summary>
public class AdminRepository : IAdminRepository
{
    private readonly AppDbContext _db;
    public AdminRepository(AppDbContext db) => _db = db;

    public async Task<ResumenAdminDto> ResumenAsync(CancellationToken ct = default)
    {
        var desdeSemana = DateTime.UtcNow.AddDays(-7);
        var hoy = DateTime.UtcNow.Date;
        var fila = (await _db.Database.SqlQuery<FilaResumen>($"""
                SELECT
                  (SELECT COUNT(*) FROM lugar WHERE estado = 'ACTIVO')                         AS LugaresActivos,
                  (SELECT COUNT(*) FROM lugar WHERE estado = 'EN_REVISION')                    AS LugaresEnRevision,
                  (SELECT COUNT(*) FROM usuario WHERE rol = 'ESTUDIANTE' AND activo = 1)       AS Estudiantes,
                  (SELECT COUNT(*) FROM comunidad_efimera WHERE inicio >= {hoy})               AS ComunidadesHoy,
                  (SELECT COUNT(*) FROM log_auditoria_rag WHERE created_at >= {desdeSemana})   AS ConsultasRagSemana,
                  (SELECT COUNT(*) FROM reporte WHERE estado = 'PENDIENTE')                    AS ReportesPendientes
                """).ToListAsync(ct)).Single();
        return new ResumenAdminDto((int)fila.LugaresActivos, (int)fila.LugaresEnRevision, (int)fila.Estudiantes,
            (int)fila.ComunidadesHoy, (int)fila.ConsultasRagSemana, (int)fila.ReportesPendientes);
    }

    public async Task<List<LugarAdminDto>> ListarLugaresAsync(CancellationToken ct = default)
    {
        var filas = await _db.Database.SqlQuery<FilaLugarAdmin>($"""
                SELECT l.id AS Id, l.nombre AS Nombre, c.nombre AS Categoria, l.estado AS Estado,
                       l.duracion_estimada_min AS DuracionEstimadaMin, l.hora_apertura AS HoraApertura,
                       l.hora_cierre AS HoraCierre,
                       (SELECT COUNT(*) FROM recomendacion r WHERE r.lugar_id = l.id) AS VecesRecomendado
                FROM lugar l JOIN categoria c ON c.id = l.categoria_id
                ORDER BY FIELD(l.estado, 'EN_REVISION', 'ACTIVO', 'INACTIVO'), l.nombre
                """).ToListAsync(ct);
        return filas.Select(f => new LugarAdminDto(f.Id, f.Nombre, f.Categoria, f.Estado, f.DuracionEstimadaMin,
            Hora(f.HoraApertura), Hora(f.HoraCierre), (int)f.VecesRecomendado)).ToList();
    }

    public async Task<string?> CambiarEstadoLugarAsync(int lugarId, string estado, int adminId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var anterior = (await _db.Database.SqlQuery<string>(
                $"SELECT estado AS Value FROM lugar WHERE id = {lugarId} FOR UPDATE").ToListAsync(ct)).FirstOrDefault();
        if (anterior is null) return null;
        if (anterior == estado)
        {
            await tx.CommitAsync(ct);
            return anterior;
        }

        await _db.Database.ExecuteSqlAsync(
            $"UPDATE lugar SET estado = {estado}, updated_at = UTC_TIMESTAMP(6), updated_by = {adminId} WHERE id = {lugarId}", ct);
        // Auditoría: quién cambió el estado y desde cuál.
        await _db.Database.ExecuteSqlAsync($"""
                INSERT INTO historial_cambio (entidad, entidad_id, accion, valores_anteriores, valores_nuevos, usuario_id)
                VALUES ('LUGAR', {lugarId}, 'UPDATE', JSON_OBJECT('estado', {anterior}), JSON_OBJECT('estado', {estado}), {adminId})
                """, ct);
        await tx.CommitAsync(ct);
        return anterior;
    }

    public async Task<List<ReporteAdminDto>> ListarReportesAsync(CancellationToken ct = default)
    {
        var filas = await _db.Database.SqlQuery<FilaReporte>($"""
                SELECT r.id AS Id, r.motivo AS Motivo, r.descripcion AS Descripcion, r.estado AS Estado,
                       COALESCE(l.nombre, c.actividad, '—') AS Objeto, u.nombre AS Autor, r.created_at AS CreadoEn
                FROM reporte r
                JOIN usuario u ON u.id = r.usuario_id
                LEFT JOIN lugar l ON l.id = r.lugar_id
                LEFT JOIN comunidad_efimera c ON c.id = r.comunidad_id
                ORDER BY FIELD(r.estado, 'PENDIENTE', 'RESUELTO', 'DESCARTADO'), r.created_at DESC
                LIMIT 100
                """).ToListAsync(ct);
        return filas.Select(f => new ReporteAdminDto(f.Id, f.Motivo, f.Descripcion, f.Estado, f.Objeto, f.Autor, f.CreadoEn)).ToList();
    }

    public async Task<List<ConsultaRagAdminDto>> ListarConsultasRagAsync(int limite, CancellationToken ct = default)
    {
        var filas = await _db.Database.SqlQuery<FilaConsultaRag>($"""
                SELECT g.id AS Id, g.created_at AS Fecha, u.nombre AS Usuario, g.tipo AS Tipo, g.entrada_usuario AS Entrada,
                       g.estado AS Estado, g.modelo AS Modelo, g.latencia_ms AS LatenciaMs,
                       (SELECT COUNT(*) FROM lugar_recuperado lr WHERE lr.log_id = g.id) AS LugaresRecuperados,
                       (SELECT COUNT(*) FROM recomendacion r WHERE r.log_id = g.id)      AS Recomendados
                FROM log_auditoria_rag g JOIN usuario u ON u.id = g.usuario_id
                ORDER BY g.created_at DESC
                LIMIT {limite}
                """).ToListAsync(ct);
        return filas.Select(f => new ConsultaRagAdminDto(f.Id, f.Fecha, f.Usuario, f.Tipo, f.Entrada, f.Estado, f.Modelo,
            f.LatenciaMs, (int)f.LugaresRecuperados, (int)f.Recomendados)).ToList();
    }

    public async Task<MetricasAdminDto> MetricasAsync(CancellationToken ct = default)
    {
        var desde = DateTime.UtcNow.AddDays(-7);
        var f = (await _db.Database.SqlQuery<FilaMetricas>($"""
                SELECT COUNT(*) AS Total,
                       COALESCE(SUM(estado = 'OK'), 0)                  AS Ok,
                       COALESCE(SUM(estado = 'SIN_RESULTADOS'), 0)      AS SinResultados,
                       COALESCE(SUM(estado = 'RECHAZADO_SEGURIDAD'), 0) AS Rechazadas,
                       COALESCE(SUM(estado = 'ERROR'), 0)               AS Errores,
                       CAST(AVG(latencia_ms) AS SIGNED)                 AS LatenciaPromedio
                FROM log_auditoria_rag WHERE created_at >= {desde}
                """).ToListAsync(ct)).Single();
        var top = await _db.Database.SqlQuery<FilaTop>($"""
                SELECT l.nombre AS Nombre, c.nombre AS Categoria, COUNT(*) AS Veces
                FROM recomendacion r
                JOIN log_auditoria_rag g ON g.id = r.log_id
                JOIN lugar l ON l.id = r.lugar_id
                JOIN categoria c ON c.id = l.categoria_id
                WHERE g.created_at >= {desde}
                GROUP BY l.id, l.nombre, c.nombre
                ORDER BY Veces DESC
                LIMIT 5
                """).ToListAsync(ct);
        return new MetricasAdminDto((int)f.Total, (int)f.Ok, (int)f.SinResultados, (int)f.Rechazadas, (int)f.Errores,
            f.LatenciaPromedio is null ? null : (int)f.LatenciaPromedio,
            top.Select(t => new LugarTopDto(t.Nombre, t.Categoria, (int)t.Veces)).ToList());
    }

    private static string? Hora(TimeSpan? t) => t is null ? null : $"{t.Value.Hours:00}:{t.Value.Minutes:00}";

    // Filas para SqlQuery: COUNT y SUM devuelven BIGINT/DECIMAL en MySQL.
    private sealed class FilaResumen
    {
        public long LugaresActivos { get; set; }
        public long LugaresEnRevision { get; set; }
        public long Estudiantes { get; set; }
        public long ComunidadesHoy { get; set; }
        public long ConsultasRagSemana { get; set; }
        public long ReportesPendientes { get; set; }
    }

    private sealed class FilaLugarAdmin
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string Estado { get; set; } = "";
        public int DuracionEstimadaMin { get; set; }
        public TimeSpan? HoraApertura { get; set; }
        public TimeSpan? HoraCierre { get; set; }
        public long VecesRecomendado { get; set; }
    }

    private sealed class FilaReporte
    {
        public int Id { get; set; }
        public string Motivo { get; set; } = "";
        public string? Descripcion { get; set; }
        public string Estado { get; set; } = "";
        public string Objeto { get; set; } = "";
        public string Autor { get; set; } = "";
        public DateTime CreadoEn { get; set; }
    }

    private sealed class FilaConsultaRag
    {
        public long Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string? Entrada { get; set; }
        public string Estado { get; set; } = "";
        public string? Modelo { get; set; }
        public int? LatenciaMs { get; set; }
        public long LugaresRecuperados { get; set; }
        public long Recomendados { get; set; }
    }

    private sealed class FilaMetricas
    {
        public long Total { get; set; }
        public decimal Ok { get; set; }
        public decimal SinResultados { get; set; }
        public decimal Rechazadas { get; set; }
        public decimal Errores { get; set; }
        public long? LatenciaPromedio { get; set; }
    }

    private sealed class FilaTop
    {
        public string Nombre { get; set; } = "";
        public string Categoria { get; set; } = "";
        public long Veces { get; set; }
    }
}
