using Breaku.Application.Horarios;

namespace Breaku.Application.Admin;

/// <summary>Casos de uso del panel de administración del campus (solo rol ADMIN).</summary>
public class AdminService
{
    public static readonly string[] EstadosLugar = ["ACTIVO", "EN_REVISION", "INACTIVO"];
    public const int LimiteConsultasPorDefecto = 50;
    public const int LimiteConsultasMaximo = 200;

    private readonly IAdminRepository _repo;
    public AdminService(IAdminRepository repo) => _repo = repo;

    public Task<ResumenAdminDto> ResumenAsync(CancellationToken ct = default) => _repo.ResumenAsync(ct);

    public Task<List<LugarAdminDto>> ListarLugaresAsync(CancellationToken ct = default) => _repo.ListarLugaresAsync(ct);

    /// <summary>Activa, pone en revisión o desactiva un lugar. Solo los ACTIVO se pueden recomendar.</summary>
    public async Task CambiarEstadoLugarAsync(int lugarId, CambioEstadoLugarDto req, int adminId, CancellationToken ct = default)
    {
        var estado = req.Estado?.Trim().ToUpperInvariant() ?? "";
        if (!EstadosLugar.Contains(estado))
            throw new ReglaNegocioException("VALIDACION", "El estado tiene que ser ACTIVO, EN_REVISION o INACTIVO.");

        var anterior = await _repo.CambiarEstadoLugarAsync(lugarId, estado, adminId, ct);
        if (anterior is null) throw new ReglaNegocioException("NO_ENCONTRADO", "El lugar no existe.");
    }

    public Task<List<ReporteAdminDto>> ListarReportesAsync(CancellationToken ct = default) => _repo.ListarReportesAsync(ct);

    public Task<List<ConsultaRagAdminDto>> ListarConsultasRagAsync(int? limite, CancellationToken ct = default)
        => _repo.ListarConsultasRagAsync(Math.Clamp(limite ?? LimiteConsultasPorDefecto, 1, LimiteConsultasMaximo), ct);

    public Task<MetricasAdminDto> MetricasAsync(CancellationToken ct = default) => _repo.MetricasAsync(ct);
}
