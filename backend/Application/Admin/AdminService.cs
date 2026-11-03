using Breaku.Application.Horarios;
using Breaku.Domain.Rules;

namespace Breaku.Application.Admin;

public class AdminService
{
    public static readonly string[] EstadosLugar = ["ACTIVO", "EN_REVISION", "INACTIVO"];
    public const int LimiteConsultasPorDefecto = 50;
    public const int LimiteConsultasMaximo = 200;

    private readonly IAdminRepository _repo;
    public AdminService(IAdminRepository repo) => _repo = repo;

    public Task<ResumenAdminDto> ResumenAsync(CancellationToken ct = default) => _repo.ResumenAsync(ct);

    public Task<List<LugarAdminDto>> ListarLugaresAsync(CancellationToken ct = default) => _repo.ListarLugaresAsync(ct);

    public async Task CambiarEstadoLugarAsync(int lugarId, CambioEstadoLugarDto req, int adminId, CancellationToken ct = default)
    {
        var estado = req.Estado?.Trim().ToUpperInvariant() ?? "";
        if (!EstadosLugar.Contains(estado))
            throw new ReglaNegocioException("VALIDACION", "El estado tiene que ser ACTIVO, EN_REVISION o INACTIVO.");

        var anterior = await _repo.CambiarEstadoLugarAsync(lugarId, estado, adminId, ct);
        if (anterior is null) throw new ReglaNegocioException("NO_ENCONTRADO", "El lugar no existe.");
    }

    public async Task CambiarValidacionLugarAsync(int lugarId, CambioValidacionLugarDto req, int adminId, CancellationToken ct = default)
    {
        if (req.Validado is not bool validado)
            throw new ReglaNegocioException("VALIDACION", "Indica si el lugar está validado.");

        var anterior = await _repo.CambiarValidacionLugarAsync(lugarId, validado, adminId, ct);
        if (anterior is null) throw new ReglaNegocioException("NO_ENCONTRADO", "El lugar no existe.");
    }

    public async Task<ResultadoValidacionMasivaDto> ValidarPendientesAsync(int adminId, CancellationToken ct = default)
        => new(await _repo.ValidarPendientesAsync(adminId, ct));

    public Task<List<ReporteAdminDto>> ListarReportesAsync(CancellationToken ct = default) => _repo.ListarReportesAsync(ct);

    public async Task ResolverReporteAsync(int reporteId, ResolverReporteDto req, int adminId, CancellationToken ct = default)
    {
        var decision = ReglasReporte.LeerDecision(req.Estado)
                       ?? throw new ReglaNegocioException("VALIDACION", ReglasReporte.MensajeDecisionInvalida);

        var anterior = await _repo.ResolverReporteAsync(reporteId, decision, adminId, ct)
                       ?? throw new ReglaNegocioException("NO_ENCONTRADO", ReglasReporte.MensajeNoExiste);
        if (!ReglasReporte.SePuedeRevisar(anterior))
            throw new ReglaNegocioException("YA_REVISADO", ReglasReporte.MensajeYaRevisado);
    }

    public Task<List<ConsultaRagAdminDto>> ListarConsultasRagAsync(int? limite, CancellationToken ct = default)
        => _repo.ListarConsultasRagAsync(Math.Clamp(limite ?? LimiteConsultasPorDefecto, 1, LimiteConsultasMaximo), ct);

    public Task<MetricasAdminDto> MetricasAsync(CancellationToken ct = default) => _repo.MetricasAsync(ct);
}
