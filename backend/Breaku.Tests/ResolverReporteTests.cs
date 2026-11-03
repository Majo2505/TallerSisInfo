using Breaku.Application.Admin;
using Breaku.Application.Horarios;
using Breaku.Domain.Rules;

namespace Breaku.Tests;

public class ReglasReporteTests
{
    [Theory]
    [InlineData("RESUELTO", "RESUELTO")]
    [InlineData("descartado", "DESCARTADO")]
    [InlineData("  Resuelto ", "RESUELTO")]
    public void Lee_la_decision_sin_importar_mayusculas_ni_espacios(string entrada, string esperada)
        => Assert.Equal(esperada, ReglasReporte.LeerDecision(entrada));

    [Theory]
    [InlineData("PENDIENTE")]
    [InlineData("BORRADO")]
    [InlineData("")]
    [InlineData(null)]
    public void Otra_decision_no_vale(string? entrada) => Assert.Null(ReglasReporte.LeerDecision(entrada));

    [Theory]
    [InlineData("PENDIENTE", true)]
    [InlineData("RESUELTO", false)]
    [InlineData("DESCARTADO", false)]
    public void Solo_se_revisa_un_reporte_pendiente(string estado, bool esperado)
        => Assert.Equal(esperado, ReglasReporte.SePuedeRevisar(estado));
}

public class ResolverReporteServiceTests
{
    private const int Admin = 2;

    private sealed class RepositorioFalso : IAdminRepository
    {
        public Dictionary<int, string> Reportes { get; } = new() { [1] = "PENDIENTE", [2] = "RESUELTO" };
        public (int Id, string Estado, int Admin)? UltimaRevision { get; private set; }

        public Task<string?> ResolverReporteAsync(int reporteId, string estado, int adminId, CancellationToken ct = default)
        {
            if (!Reportes.TryGetValue(reporteId, out var anterior)) return Task.FromResult<string?>(null);
            if (anterior == "PENDIENTE")
            {
                Reportes[reporteId] = estado;
                UltimaRevision = (reporteId, estado, adminId);
            }
            return Task.FromResult<string?>(anterior);
        }

        public Task<ResumenAdminDto> ResumenAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<LugarAdminDto>> ListarLugaresAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<string?> CambiarEstadoLugarAsync(int lugarId, string estado, int adminId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<ReporteAdminDto>> ListarReportesAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<ConsultaRagAdminDto>> ListarConsultasRagAsync(int limite, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<MetricasAdminDto> MetricasAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }

    private static (AdminService Servicio, RepositorioFalso Repo) Crear()
    {
        var repo = new RepositorioFalso();
        return (new AdminService(repo), repo);
    }

    [Fact]
    public async Task Resuelve_un_reporte_pendiente_y_registra_al_revisor()
    {
        var (servicio, repo) = Crear();

        await servicio.ResolverReporteAsync(1, new ResolverReporteDto("resuelto"), Admin);

        Assert.Equal((1, "RESUELTO", Admin), repo.UltimaRevision);
    }

    [Fact]
    public async Task Un_reporte_ya_revisado_no_se_cambia()
    {
        var (servicio, repo) = Crear();

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            servicio.ResolverReporteAsync(2, new ResolverReporteDto("DESCARTADO"), Admin));

        Assert.Equal("YA_REVISADO", ex.Codigo);
        Assert.Equal("RESUELTO", repo.Reportes[2]);
    }

    [Fact]
    public async Task Un_reporte_que_no_existe_responde_no_encontrado()
    {
        var (servicio, _) = Crear();
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            servicio.ResolverReporteAsync(99, new ResolverReporteDto("RESUELTO"), Admin));
        Assert.Equal("NO_ENCONTRADO", ex.Codigo);
    }

    [Theory]
    [InlineData("PENDIENTE")]
    [InlineData(null)]
    public async Task Una_decision_invalida_no_llega_a_la_base(string? estado)
    {
        var (servicio, repo) = Crear();

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            servicio.ResolverReporteAsync(1, new ResolverReporteDto(estado), Admin));

        Assert.Equal("VALIDACION", ex.Codigo);
        Assert.Null(repo.UltimaRevision);
    }
}
