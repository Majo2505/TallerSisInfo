using Breaku.Application.Sanciones;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

/// <summary>Una transacción sobre el mismo <see cref="AppDbContext"/> de la petición: los repositorios participan sin enterarse.</summary>
public sealed class TransaccionEf : ITransaccion
{
    private readonly AppDbContext _db;
    public TransaccionEf(AppDbContext db) => _db = db;

    public async Task<T> EjecutarAsync<T>(Func<Task<T>> trabajo, CancellationToken ct = default)
    {
        if (_db.Database.CurrentTransaction is not null) return await trabajo();

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var resultado = await trabajo();
        await tx.CommitAsync(ct);
        return resultado;
    }
}

/// <summary>HU28: quiénes están suspendidos ahora, para ocultar lo suyo a los demás estudiantes.</summary>
internal static class FiltrosSancion
{
    /// <summary>Ids de los usuarios con una suspensión vigente (sin levantar y con la fecha de fin por llegar). La baja ya sale por <c>activo = 0</c>.</summary>
    public static IQueryable<int> IdsSuspendidos(AppDbContext db, DateTime ahoraUtc)
        => db.Sanciones
            .Where(s => s.Tipo == Breaku.Domain.Sancion.TipoSuspension && s.LevantadaEn == null && s.Hasta > ahoraUtc)
            .Select(s => s.UsuarioId);
}
