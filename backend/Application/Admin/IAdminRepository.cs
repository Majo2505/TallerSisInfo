namespace Breaku.Application.Admin;

public interface IAdminRepository
{
    Task<ResumenAdminDto> ResumenAsync(CancellationToken ct = default);
    Task<List<LugarAdminDto>> ListarLugaresAsync(CancellationToken ct = default);

    Task<string?> CambiarEstadoLugarAsync(int lugarId, string estado, int adminId, CancellationToken ct = default);

    Task<List<ReporteAdminDto>> ListarReportesAsync(CancellationToken ct = default);
    Task<string?> ResolverReporteAsync(int reporteId, string estado, int adminId, CancellationToken ct = default);
    Task<List<ConsultaRagAdminDto>> ListarConsultasRagAsync(int limite, CancellationToken ct = default);
    Task<MetricasAdminDto> MetricasAsync(CancellationToken ct = default);
}
