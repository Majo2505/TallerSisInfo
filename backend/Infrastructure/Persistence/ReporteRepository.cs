using Breaku.Application.Reportes;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

/// <summary>Reportes de los estudiantes. Con SQL, como el panel admin que los lee (la tabla reporte no está mapeada).</summary>
public class ReporteRepository : IReporteRepository
{
    private readonly AppDbContext _db;
    public ReporteRepository(AppDbContext db) => _db = db;

    public async Task<List<LugarReportableDto>> ListarLugaresAsync(CancellationToken ct = default)
    {
        var filas = await _db.Database.SqlQuery<FilaLugar>($"""
                SELECT id AS Id, nombre AS Nombre FROM lugar WHERE estado = 'ACTIVO' ORDER BY nombre
                """).ToListAsync(ct);
        return filas.Select(f => new LugarReportableDto(f.Id, f.Nombre)).ToList();
    }

    public async Task<bool> LugarExisteAsync(int lugarId, CancellationToken ct = default)
        => (await _db.Database.SqlQuery<long>($"SELECT COUNT(*) AS Value FROM lugar WHERE id = {lugarId}").ToListAsync(ct)).Single() > 0;

    public async Task<int> ContarPendientesAsync(int usuarioId, CancellationToken ct = default)
        => (int)(await _db.Database.SqlQuery<long>($"""
                SELECT COUNT(*) AS Value FROM reporte WHERE usuario_id = {usuarioId} AND estado = 'PENDIENTE'
                """).ToListAsync(ct)).Single();

    public async Task<bool> TienePendienteDelLugarAsync(int usuarioId, int lugarId, CancellationToken ct = default)
        => (await _db.Database.SqlQuery<long>($"""
                SELECT COUNT(*) AS Value FROM reporte WHERE usuario_id = {usuarioId} AND lugar_id = {lugarId} AND estado = 'PENDIENTE'
                """).ToListAsync(ct)).Single() > 0;

    public async Task<int> CrearAsync(int usuarioId, string motivo, int? lugarId, string descripcion, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        await _db.Database.ExecuteSqlAsync($"""
                INSERT INTO reporte (usuario_id, lugar_id, motivo, descripcion) VALUES ({usuarioId}, {lugarId}, {motivo}, {descripcion})
                """, ct);
        var id = (int)(await _db.Database.SqlQuery<ulong>($"SELECT LAST_INSERT_ID() AS Value").ToListAsync(ct)).Single();
        await tx.CommitAsync(ct);
        return id;
    }

    private sealed class FilaLugar
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
    }
}
