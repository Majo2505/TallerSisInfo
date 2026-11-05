using Breaku.Domain.Rules;

namespace Breaku.Application.Admin;

public interface IAdminRepository
{
    Task<ResumenAdminDto> ResumenAsync(CancellationToken ct = default);
    Task<List<LugarAdminDto>> ListarLugaresAsync(CancellationToken ct = default);

    Task<string?> CambiarEstadoLugarAsync(int lugarId, string estado, int adminId, CancellationToken ct = default);

    Task<bool?> CambiarValidacionLugarAsync(int lugarId, bool validado, int adminId, CancellationToken ct = default);

    Task<List<CategoriaAdminDto>> ListarCategoriasAsync(CancellationToken ct = default);
    Task<List<CampusAdminDto>> ListarCampusActivosAsync(CancellationToken ct = default);
    Task<LugarDetalleAdminDto?> ObtenerLugarAsync(int lugarId, CancellationToken ct = default);

    /// <summary>true si otro lugar (distinto de <paramref name="excluirId"/>) ya tiene ese nombre.</summary>
    Task<bool> NombreEnUsoAsync(string nombre, int? excluirId, CancellationToken ct = default);

    /// <summary>Crea el lugar ACTIVO y validado, y deja el CREATE en historial_cambio. Devuelve el id.</summary>
    Task<int> CrearLugarAsync(DatosLugar datos, int adminId, CancellationToken ct = default);

    /// <summary>Edita el lugar y deja el antes y el después en historial_cambio. false si no existe.</summary>
    Task<bool> EditarLugarAsync(int lugarId, DatosLugar datos, int adminId, CancellationToken ct = default);

    /// <summary>true si el lugar se usó en recomendaciones, reportes o comunidades (no se puede eliminar).</summary>
    Task<bool> LugarEnUsoAsync(int lugarId, CancellationToken ct = default);

    /// <summary>Elimina el lugar y deja el DELETE en historial_cambio. false si no existe.</summary>
    Task<bool> EliminarLugarAsync(int lugarId, int adminId, CancellationToken ct = default);

    Task<List<ReporteAdminDto>> ListarReportesAsync(CancellationToken ct = default);
    Task<string?> ResolverReporteAsync(int reporteId, string estado, int adminId, CancellationToken ct = default);
    Task<List<ConsultaRagAdminDto>> ListarConsultasRagAsync(int limite, CancellationToken ct = default);
    Task<MetricasAdminDto> MetricasAsync(CancellationToken ct = default);
}
