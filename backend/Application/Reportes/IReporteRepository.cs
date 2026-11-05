namespace Breaku.Application.Reportes;

/// <summary>Puerto: lo implementa Infrastructure.</summary>
public interface IReporteRepository
{
    /// <summary>Lugares activos del catálogo, por nombre (para elegir cuál reportar).</summary>
    Task<List<LugarReportableDto>> ListarLugaresAsync(CancellationToken ct = default);

    Task<bool> LugarExisteAsync(int lugarId, CancellationToken ct = default);

    /// <summary>Cuántos reportes PENDIENTE tiene el usuario.</summary>
    Task<int> ContarPendientesAsync(int usuarioId, CancellationToken ct = default);

    /// <summary>Si el usuario ya tiene un reporte PENDIENTE de ese lugar.</summary>
    Task<bool> TienePendienteDelLugarAsync(int usuarioId, int lugarId, CancellationToken ct = default);

    /// <summary>Guarda el reporte PENDIENTE y devuelve su id.</summary>
    Task<int> CrearAsync(int usuarioId, string motivo, int? lugarId, string descripcion, CancellationToken ct = default);
}
