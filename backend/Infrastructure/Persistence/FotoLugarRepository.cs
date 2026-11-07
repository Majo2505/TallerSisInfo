using Breaku.Application.Lugares;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

public class FotoLugarRepository : IFotoLugarRepository
{
    private readonly AppDbContext _db;
    public FotoLugarRepository(AppDbContext db) => _db = db;

    public async Task<FotoLugar?> ObtenerAsync(int lugarId, CancellationToken ct = default)
    {
        var fila = (await _db.Database.SqlQuery<FilaFoto>($"""
                SELECT contenido AS Contenido, tipo_mime AS TipoMime, actualizada_en AS ActualizadaEn
                FROM lugar_foto WHERE lugar_id = {lugarId}
                """).ToListAsync(ct)).FirstOrDefault();
        return fila is null ? null : new FotoLugar(fila.Contenido, fila.TipoMime, DateTime.SpecifyKind(fila.ActualizadaEn, DateTimeKind.Utc));
    }

    public async Task<bool> GuardarAsync(int lugarId, byte[] contenido, string tipoMime, DateTime version, int adminId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        if (!await BloquearLugarAsync(lugarId, ct)) return false;

        var teniaFoto = await TieneFotoAsync(lugarId, ct);
        await _db.Database.ExecuteSqlAsync($"""
                INSERT INTO lugar_foto (lugar_id, contenido, tipo_mime, bytes, actualizada_en, actualizada_por)
                VALUES ({lugarId}, {contenido}, {tipoMime}, {contenido.Length}, {version}, {adminId})
                ON DUPLICATE KEY UPDATE contenido = VALUES(contenido), tipo_mime = VALUES(tipo_mime), bytes = VALUES(bytes),
                                        actualizada_en = VALUES(actualizada_en), actualizada_por = VALUES(actualizada_por)
                """, ct);
        await _db.Database.ExecuteSqlAsync($"""
                UPDATE lugar SET foto_actualizada_en = {version}, updated_at = UTC_TIMESTAMP(6), updated_by = {adminId}
                WHERE id = {lugarId}
                """, ct);
        await HistorialAsync(lugarId, teniaFoto ? "anterior" : "ninguna", "nueva", adminId, ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<bool> EliminarAsync(int lugarId, int adminId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        if (!await BloquearLugarAsync(lugarId, ct)) return false;

        if (await TieneFotoAsync(lugarId, ct))
        {
            await _db.Database.ExecuteSqlAsync($"DELETE FROM lugar_foto WHERE lugar_id = {lugarId}", ct);
            await _db.Database.ExecuteSqlAsync($"""
                    UPDATE lugar SET foto_actualizada_en = NULL, updated_at = UTC_TIMESTAMP(6), updated_by = {adminId}
                    WHERE id = {lugarId}
                    """, ct);
            await HistorialAsync(lugarId, "anterior", "ninguna", adminId, ct);
        }

        await tx.CommitAsync(ct);
        return true;
    }

    private async Task<bool> BloquearLugarAsync(int lugarId, CancellationToken ct)
        => (await _db.Database.SqlQuery<int>($"SELECT id AS Value FROM lugar WHERE id = {lugarId} FOR UPDATE").ToListAsync(ct)).Count > 0;

    private async Task<bool> TieneFotoAsync(int lugarId, CancellationToken ct)
        => (await _db.Database.SqlQuery<long>($"SELECT COUNT(*) AS Value FROM lugar_foto WHERE lugar_id = {lugarId}").ToListAsync(ct)).Single() > 0;

    // En el historial queda qué pasó con la foto y quién lo hizo; los bytes no.
    private Task<int> HistorialAsync(int lugarId, string antes, string despues, int adminId, CancellationToken ct)
        => _db.Database.ExecuteSqlAsync($"""
                INSERT INTO historial_cambio (entidad, entidad_id, accion, valores_anteriores, valores_nuevos, usuario_id)
                VALUES ('LUGAR', {lugarId}, 'UPDATE', JSON_OBJECT('foto', {antes}), JSON_OBJECT('foto', {despues}), {adminId})
                """, ct);

    private sealed class FilaFoto
    {
        public byte[] Contenido { get; set; } = [];
        public string TipoMime { get; set; } = "";
        public DateTime ActualizadaEn { get; set; }
    }
}
