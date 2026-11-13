using System.Text.Json;
using Breaku.Application.Horarios;
using Breaku.Domain.Rules;
using MySqlConnector;
using Breaku.Application.Admin;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

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
                  (SELECT COUNT(DISTINCT COALESCE(consulta_origen_id, id)) FROM log_auditoria_rag
                    WHERE created_at >= {desdeSemana})                                          AS ConsultasRagSemana,
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
                       l.hora_cierre AS HoraCierre, l.validado AS Validado, l.recomendable AS Recomendable,
                       l.tipo_lugar AS TipoLugar,
                       (SELECT COUNT(*) FROM recomendacion r WHERE r.lugar_id = l.id) AS VecesRecomendado
                FROM lugar l JOIN categoria c ON c.id = l.categoria_id
                ORDER BY FIELD(l.estado, 'EN_REVISION', 'ACTIVO', 'INACTIVO'), l.nombre
                """).ToListAsync(ct);
        return filas.Select(f => new LugarAdminDto(f.Id, f.Nombre, f.Categoria, f.Estado, f.DuracionEstimadaMin,
            Hora(f.HoraApertura), Hora(f.HoraCierre), (int)f.VecesRecomendado, f.Validado, f.Recomendable, f.TipoLugar)).ToList();
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
        await _db.Database.ExecuteSqlAsync($"""
                INSERT INTO historial_cambio (entidad, entidad_id, accion, valores_anteriores, valores_nuevos, usuario_id)
                VALUES ('LUGAR', {lugarId}, 'UPDATE', JSON_OBJECT('estado', {anterior}), JSON_OBJECT('estado', {estado}), {adminId})
                """, ct);
        await tx.CommitAsync(ct);
        return anterior;
    }

    public async Task<bool?> CambiarValidacionLugarAsync(int lugarId, bool validado, int adminId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var filas = await _db.Database.SqlQuery<long>(
            $"SELECT CAST(validado AS SIGNED) AS Value FROM lugar WHERE id = {lugarId} FOR UPDATE").ToListAsync(ct);
        if (filas.Count == 0) return null;
        var anterior = filas[0] != 0;
        if (anterior == validado)
        {
            await tx.CommitAsync(ct);
            return anterior;
        }

        await _db.Database.ExecuteSqlAsync(
            $"UPDATE lugar SET validado = {validado}, updated_at = UTC_TIMESTAMP(6), updated_by = {adminId} WHERE id = {lugarId}", ct);
        var textoAnterior = anterior ? "true" : "false";
        var textoNuevo = validado ? "true" : "false";
        await _db.Database.ExecuteSqlAsync($"""
                INSERT INTO historial_cambio (entidad, entidad_id, accion, valores_anteriores, valores_nuevos, usuario_id)
                VALUES ('LUGAR', {lugarId}, 'UPDATE', JSON_OBJECT('validado', CAST({textoAnterior} AS JSON)), JSON_OBJECT('validado', CAST({textoNuevo} AS JSON)), {adminId})
                """, ct);
        await tx.CommitAsync(ct);
        return anterior;
    }

    public async Task<List<CategoriaAdminDto>> ListarCategoriasAsync(CancellationToken ct = default)
    {
        var filas = await _db.Database.SqlQuery<FilaCategoria>($"SELECT id AS Id, nombre AS Nombre FROM categoria ORDER BY nombre")
            .ToListAsync(ct);
        return filas.Select(f => new CategoriaAdminDto(f.Id, f.Nombre)).ToList();
    }

    public async Task<List<CampusAdminDto>> ListarCampusActivosAsync(CancellationToken ct = default)
    {
        var filas = await _db.Database.SqlQuery<FilaCampus>($"""
                SELECT id AS Id, nombre AS Nombre, latitud AS Latitud, longitud AS Longitud, radio_max_m AS RadioMaxM
                FROM zona WHERE activa = TRUE ORDER BY nombre
                """).ToListAsync(ct);
        return filas.Select(f => new CampusAdminDto(f.Id, f.Nombre, f.Latitud, f.Longitud, f.RadioMaxM)).ToList();
    }

    public async Task<LugarDetalleAdminDto?> ObtenerLugarAsync(int lugarId, CancellationToken ct = default)
    {
        var f = await LeerLugarAsync(lugarId, bloquear: false, ct);
        return f is null ? null : new LugarDetalleAdminDto(f.Id, f.Nombre, f.Descripcion, f.CategoriaId, f.Latitud, f.Longitud,
            f.DuracionEstimadaMin, Hora(f.HoraApertura), Hora(f.HoraCierre), f.MinAPieDesdeBloqueA, f.Estado, f.Validado,
            ReglasFotoLugar.Url(f.Id, f.FotoActualizadaEn is { } v ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : null));
    }

    public async Task<bool> NombreEnUsoAsync(string nombre, int? excluirId, CancellationToken ct = default)
        => (await _db.Database.SqlQuery<long>($"""
                SELECT COUNT(*) AS Value FROM lugar WHERE nombre = {nombre} AND ({excluirId} IS NULL OR id <> {excluirId})
                """).ToListAsync(ct)).Single() > 0;

    public async Task<int> CrearLugarAsync(DatosLugar d, int adminId, CancellationToken ct = default)
    {
        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            await _db.Database.ExecuteSqlAsync($"""
                    INSERT INTO lugar (nombre, descripcion, categoria_id, latitud, longitud, duracion_estimada_min, hora_apertura,
                                       hora_cierre, min_a_pie_desde_bloque_a, estado, recomendable, validado, created_by, updated_by)
                    VALUES ({d.Nombre}, {d.Descripcion}, {d.CategoriaId}, {d.Latitud}, {d.Longitud}, {d.DuracionEstimadaMin},
                            {d.HoraApertura?.ToTimeSpan()}, {d.HoraCierre?.ToTimeSpan()}, {d.MinAPieDesdeBloqueA},
                            'ACTIVO', TRUE, TRUE, {adminId}, {adminId})
                    """, ct);
            var id = (int)(await _db.Database.SqlQuery<ulong>($"SELECT LAST_INSERT_ID() AS Value").ToListAsync(ct)).Single();
            await HistorialLugarAsync(id, "CREATE", null, JsonLugar(d), adminId, ct);
            await tx.CommitAsync(ct);
            return id;
        }
        catch (Exception ex) when (EsNombreRepetido(ex))
        {
            // Dos admins guardan el mismo nombre a la vez: la base (uq_lugar_nombre) deja pasar solo uno.
            throw new ReglaNegocioException("NOMBRE_REPETIDO", ReglasLugar.MensajeNombreRepetido);
        }
    }

    public async Task<bool> EditarLugarAsync(int lugarId, DatosLugar d, int adminId, CancellationToken ct = default)
    {
        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            var antes = await LeerLugarAsync(lugarId, bloquear: true, ct);
            if (antes is null) return false;

            await _db.Database.ExecuteSqlAsync($"""
                    UPDATE lugar SET nombre = {d.Nombre}, descripcion = {d.Descripcion}, categoria_id = {d.CategoriaId},
                           latitud = {d.Latitud}, longitud = {d.Longitud}, duracion_estimada_min = {d.DuracionEstimadaMin},
                           hora_apertura = {d.HoraApertura?.ToTimeSpan()}, hora_cierre = {d.HoraCierre?.ToTimeSpan()},
                           min_a_pie_desde_bloque_a = {d.MinAPieDesdeBloqueA}, updated_at = UTC_TIMESTAMP(6), updated_by = {adminId}
                    WHERE id = {lugarId}
                    """, ct);
            await HistorialLugarAsync(lugarId, "UPDATE", JsonLugar(antes.ADatos()), JsonLugar(d), adminId, ct);
            await tx.CommitAsync(ct);
            return true;
        }
        catch (Exception ex) when (EsNombreRepetido(ex))
        {
            throw new ReglaNegocioException("NOMBRE_REPETIDO", ReglasLugar.MensajeNombreRepetido);
        }
    }

    public async Task<bool> LugarEnUsoAsync(int lugarId, CancellationToken ct = default)
        => (await _db.Database.SqlQuery<long>($"""
                SELECT (SELECT COUNT(*) FROM lugar_recuperado WHERE lugar_id = {lugarId})
                     + (SELECT COUNT(*) FROM recomendacion     WHERE lugar_id = {lugarId})
                     + (SELECT COUNT(*) FROM reporte           WHERE lugar_id = {lugarId})
                     + (SELECT COUNT(*) FROM comunidad_efimera WHERE lugar_id = {lugarId}) AS Value
                """).ToListAsync(ct)).Single() > 0;

    public async Task<bool> EliminarLugarAsync(int lugarId, int adminId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var antes = await LeerLugarAsync(lugarId, bloquear: true, ct);
        if (antes is null) return false;

        await _db.Database.ExecuteSqlAsync($"DELETE FROM lugar WHERE id = {lugarId}", ct);
        await HistorialLugarAsync(lugarId, "DELETE", JsonLugar(antes.ADatos()), null, adminId, ct);
        await tx.CommitAsync(ct);
        return true;
    }

    private async Task<FilaLugarDetalle?> LeerLugarAsync(int lugarId, bool bloquear, CancellationToken ct)
    {
        var consulta = bloquear
            ? _db.Database.SqlQuery<FilaLugarDetalle>($"""
                SELECT id AS Id, nombre AS Nombre, descripcion AS Descripcion, categoria_id AS CategoriaId, latitud AS Latitud,
                       longitud AS Longitud, duracion_estimada_min AS DuracionEstimadaMin, hora_apertura AS HoraApertura,
                       hora_cierre AS HoraCierre, min_a_pie_desde_bloque_a AS MinAPieDesdeBloqueA, estado AS Estado, validado AS Validado,
                       foto_actualizada_en AS FotoActualizadaEn
                FROM lugar WHERE id = {lugarId} FOR UPDATE
                """)
            : _db.Database.SqlQuery<FilaLugarDetalle>($"""
                SELECT id AS Id, nombre AS Nombre, descripcion AS Descripcion, categoria_id AS CategoriaId, latitud AS Latitud,
                       longitud AS Longitud, duracion_estimada_min AS DuracionEstimadaMin, hora_apertura AS HoraApertura,
                       hora_cierre AS HoraCierre, min_a_pie_desde_bloque_a AS MinAPieDesdeBloqueA, estado AS Estado, validado AS Validado,
                       foto_actualizada_en AS FotoActualizadaEn
                FROM lugar WHERE id = {lugarId}
                """);
        return (await consulta.ToListAsync(ct)).FirstOrDefault();
    }

    // El historial sobrevive al lugar (entidad_id sin FK): queda quién lo creó, editó o eliminó y con qué datos.
    private Task<int> HistorialLugarAsync(int lugarId, string accion, string? antes, string? despues, int adminId, CancellationToken ct)
        => _db.Database.ExecuteSqlAsync($"""
                INSERT INTO historial_cambio (entidad, entidad_id, accion, valores_anteriores, valores_nuevos, usuario_id)
                VALUES ('LUGAR', {lugarId}, {accion}, CAST({antes} AS JSON), CAST({despues} AS JSON), {adminId})
                """, ct);

    private static string JsonLugar(DatosLugar d) => JsonSerializer.Serialize(new
    {
        nombre = d.Nombre,
        descripcion = d.Descripcion,
        categoria_id = d.CategoriaId,
        latitud = d.Latitud,
        longitud = d.Longitud,
        duracion_estimada_min = d.DuracionEstimadaMin,
        hora_apertura = d.HoraApertura?.ToString("HH:mm"),
        hora_cierre = d.HoraCierre?.ToString("HH:mm"),
        min_a_pie_desde_bloque_a = d.MinAPieDesdeBloqueA
    });

    private static bool EsNombreRepetido(Exception ex)
        => (ex as MySqlException ?? ex.InnerException as MySqlException) is { ErrorCode: MySqlErrorCode.DuplicateKeyEntry } m
           && m.Message.Contains("uq_lugar_nombre");

    public async Task<List<ReporteAdminDto>> ListarReportesAsync(CancellationToken ct = default)
    {
        var filas = await _db.Database.SqlQuery<FilaReporte>($"""
                SELECT r.id AS Id, r.motivo AS Motivo, r.descripcion AS Descripcion, r.estado AS Estado,
                       COALESCE(l.nombre, c.actividad, e.titulo, 'General') AS Objeto, u.nombre AS Autor, r.created_at AS CreadoEn,
                       rep.id AS UsuarioReportadoId, rep.nombre AS UsuarioReportadoNombre
                FROM reporte r
                JOIN usuario u ON u.id = r.usuario_id
                LEFT JOIN lugar l ON l.id = r.lugar_id
                LEFT JOIN comunidad_efimera c ON c.id = r.comunidad_id
                LEFT JOIN evento e ON e.id = r.evento_id
                LEFT JOIN usuario rep ON rep.id = COALESCE(c.creador_id, e.creador_id) AND rep.rol = 'ESTUDIANTE'
                ORDER BY FIELD(r.estado, 'PENDIENTE', 'RESUELTO', 'DESCARTADO'), r.created_at DESC
                LIMIT 100
                """).ToListAsync(ct);
        return filas.Select(f => new ReporteAdminDto(f.Id, f.Motivo, f.Descripcion, f.Estado, f.Objeto, f.Autor, f.CreadoEn,
            f.UsuarioReportadoId, f.UsuarioReportadoNombre)).ToList();
    }

    public async Task<string?> ResolverReporteAsync(int reporteId, string estado, int adminId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var anterior = (await _db.Database.SqlQuery<string>(
            $"SELECT estado AS Value FROM reporte WHERE id = {reporteId} FOR UPDATE").ToListAsync(ct)).FirstOrDefault();
        if (anterior is null) return null;
        if (anterior != "PENDIENTE") return anterior;

        await _db.Database.ExecuteSqlAsync($"""
            UPDATE reporte
            SET estado = {estado}, revisor_id = {adminId}, revisado_en = UTC_TIMESTAMP(6)
            WHERE id = {reporteId} AND estado = 'PENDIENTE'
            """, ct);
        await tx.CommitAsync(ct);
        return anterior;
    }

    public async Task<List<TurnoConsultaRag>> ListarConsultasRagAsync(int limite, CancellationToken ct = default)
    {
        // Primero las últimas conversaciones (por su turno más reciente) y después todos sus turnos.
        var filas = await _db.Database.SqlQuery<FilaConsultaRag>($"""
                SELECT g.id AS Id, COALESCE(g.consulta_origen_id, g.id) AS Conversacion, g.created_at AS Fecha,
                       u.nombre AS Usuario, g.tipo AS Tipo, g.entrada_usuario AS Entrada,
                       g.estado AS Estado, g.motivo_rechazo AS MotivoRechazo, g.modelo AS Modelo, g.latencia_ms AS LatenciaMs,
                       (SELECT COUNT(*) FROM lugar_recuperado lr WHERE lr.log_id = g.id) AS LugaresRecuperados,
                       (SELECT COUNT(*) FROM recomendacion r WHERE r.log_id = g.id)      AS Recomendados
                FROM log_auditoria_rag g
                JOIN usuario u ON u.id = g.usuario_id
                JOIN (SELECT COALESCE(consulta_origen_id, id) AS conversacion, MAX(created_at) AS ultima
                      FROM log_auditoria_rag
                      GROUP BY COALESCE(consulta_origen_id, id)
                      ORDER BY ultima DESC
                      LIMIT {limite}) ultimas ON ultimas.conversacion = COALESCE(g.consulta_origen_id, g.id)
                ORDER BY g.created_at, g.id
                """).ToListAsync(ct);
        return filas.Select(f => new TurnoConsultaRag(f.Id, f.Conversacion, f.Fecha, f.Usuario, f.Tipo, f.Entrada, f.Estado,
            f.MotivoRechazo, f.Modelo, f.LatenciaMs, (int)f.LugaresRecuperados, (int)f.Recomendados)).ToList();
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
                SELECT l.nombre AS Nombre, c.nombre AS Categoria, COUNT(*) AS Veces,
                       COALESCE(SUM(r.feedback = 'LIKE'), 0)    AS MeGusta,
                       COALESCE(SUM(r.feedback = 'DISLIKE'), 0) AS NoMeSirve
                FROM recomendacion r
                JOIN log_auditoria_rag g ON g.id = r.log_id
                JOIN lugar l ON l.id = r.lugar_id
                JOIN categoria c ON c.id = l.categoria_id
                WHERE g.created_at >= {desde}
                GROUP BY l.id, l.nombre, c.nombre
                ORDER BY Veces DESC
                LIMIT 5
                """).ToListAsync(ct);
        var votos = (await _db.Database.SqlQuery<FilaVotos>($"""
                SELECT COALESCE(SUM(feedback = 'LIKE'), 0)    AS MeGusta,
                       COALESCE(SUM(feedback = 'DISLIKE'), 0) AS NoMeSirve
                FROM recomendacion WHERE feedback IS NOT NULL AND fecha_feedback >= {desde}
                """).ToListAsync(ct)).Single();
        var rechazos = await _db.Database.SqlQuery<FilaRechazo>($"""
                SELECT COALESCE(motivo_rechazo, 'SIN_MOTIVO') AS Motivo, COUNT(*) AS Cantidad
                FROM log_auditoria_rag
                WHERE estado = 'RECHAZADO_SEGURIDAD' AND created_at >= {desde}
                GROUP BY COALESCE(motivo_rechazo, 'SIN_MOTIVO')
                ORDER BY Cantidad DESC
                """).ToListAsync(ct);
        var tiempos = await _db.Database.SqlQuery<FilaTiempo>($"""
                SELECT latencia_ms AS LatenciaMs, modelo = {ReglasRendimiento.ModeloRespaldo} AS Respaldo
                FROM log_auditoria_rag
                WHERE tipo = 'RECOMENDACION' AND latencia_ms IS NOT NULL AND created_at >= {desde}
                """).ToListAsync(ct);
        var rendimiento = RendimientoRecomendacion.Calcular(tiempos.Select(t => (t.LatenciaMs, t.Respaldo == true)).ToList());
        return new MetricasAdminDto((int)f.Total, (int)f.Ok, (int)f.SinResultados, (int)f.Rechazadas, (int)f.Errores,
            f.LatenciaPromedio is null ? null : (int)f.LatenciaPromedio,
            top.Select(t => new LugarTopDto(t.Nombre, t.Categoria, (int)t.Veces, (int)t.MeGusta, (int)t.NoMeSirve)).ToList(),
            (int)votos.MeGusta, (int)votos.NoMeSirve,
            rechazos.Select(r => new RechazoPorMotivoDto(r.Motivo, (int)r.Cantidad)).ToList(), rendimiento);
    }

    private sealed class FilaTiempo
    {
        public int LatenciaMs { get; set; }
        public bool? Respaldo { get; set; }
    }

    private static string? Hora(TimeSpan? t) => t is null ? null : $"{t.Value.Hours:00}:{t.Value.Minutes:00}";

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
        public bool Validado { get; set; }
        public bool Recomendable { get; set; }
        public string? TipoLugar { get; set; }
        public long VecesRecomendado { get; set; }
    }

    private sealed class FilaCategoria
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
    }

    private sealed class FilaCampus
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
        public int RadioMaxM { get; set; }
    }

    private sealed class FilaLugarDetalle
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string? Descripcion { get; set; }
        public int CategoriaId { get; set; }
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
        public short DuracionEstimadaMin { get; set; }
        public TimeSpan? HoraApertura { get; set; }
        public TimeSpan? HoraCierre { get; set; }
        public short? MinAPieDesdeBloqueA { get; set; }
        public string Estado { get; set; } = "";
        public bool Validado { get; set; }
        public DateTime? FotoActualizadaEn { get; set; }

        public DatosLugar ADatos() => new(Nombre, Descripcion, CategoriaId, Latitud, Longitud, DuracionEstimadaMin,
            HoraApertura is { } a ? TimeOnly.FromTimeSpan(a) : null, HoraCierre is { } c ? TimeOnly.FromTimeSpan(c) : null,
            MinAPieDesdeBloqueA);
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
        public int? UsuarioReportadoId { get; set; }
        public string? UsuarioReportadoNombre { get; set; }
    }

    private sealed class FilaConsultaRag
    {
        public long Id { get; set; }
        public long Conversacion { get; set; }
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string? Entrada { get; set; }
        public string Estado { get; set; } = "";
        public string? MotivoRechazo { get; set; }
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
        public decimal MeGusta { get; set; }
        public decimal NoMeSirve { get; set; }
    }

    private sealed class FilaRechazo
    {
        public string Motivo { get; set; } = "";
        public long Cantidad { get; set; }
    }

    private sealed class FilaVotos
    {
        public decimal MeGusta { get; set; }
        public decimal NoMeSirve { get; set; }
    }
}
