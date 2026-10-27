namespace Breaku.Application.Admin;

/// <summary>Lecturas del panel de administración y el cambio de estado de un lugar.</summary>
public interface IAdminRepository
{
    Task<ResumenAdminDto> ResumenAsync(CancellationToken ct = default);
    Task<List<LugarAdminDto>> ListarLugaresAsync(CancellationToken ct = default);

    /// <summary>Devuelve el estado anterior, o <c>null</c> si el lugar no existe.</summary>
    Task<string?> CambiarEstadoLugarAsync(int lugarId, string estado, int adminId, CancellationToken ct = default);

    Task<List<ReporteAdminDto>> ListarReportesAsync(CancellationToken ct = default);
    Task<List<ConsultaRagAdminDto>> ListarConsultasRagAsync(int limite, CancellationToken ct = default);
    Task<MetricasAdminDto> MetricasAsync(CancellationToken ct = default);
}
