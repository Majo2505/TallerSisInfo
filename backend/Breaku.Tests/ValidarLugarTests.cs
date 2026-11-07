using Breaku.Application.Admin;
using Breaku.Application.Horarios;

namespace Breaku.Tests;

public class ValidarLugarTests
{
    private const int Admin = 2;

    private sealed class RepositorioFalso : IAdminRepository
    {
        public Dictionary<int, bool> Validados { get; } = new() { [1] = false, [2] = true };
        public int? AdminQueValido { get; private set; }

        public Task<bool?> CambiarValidacionLugarAsync(int lugarId, bool validado, int adminId, CancellationToken ct = default)
        {
            if (!Validados.TryGetValue(lugarId, out var anterior)) return Task.FromResult<bool?>(null);
            Validados[lugarId] = validado;
            AdminQueValido = adminId;
            return Task.FromResult<bool?>(anterior);
        }

        public Task<ResumenAdminDto> ResumenAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<LugarAdminDto>> ListarLugaresAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<string?> CambiarEstadoLugarAsync(int lugarId, string estado, int adminId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<ReporteAdminDto>> ListarReportesAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<CategoriaAdminDto>> ListarCategoriasAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<CampusAdminDto>> ListarCampusActivosAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<LugarDetalleAdminDto?> ObtenerLugarAsync(int lugarId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> NombreEnUsoAsync(string nombre, int? excluirId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> CrearLugarAsync(Breaku.Domain.Rules.DatosLugar datos, int adminId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> EditarLugarAsync(int lugarId, Breaku.Domain.Rules.DatosLugar datos, int adminId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> LugarEnUsoAsync(int lugarId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> EliminarLugarAsync(int lugarId, int adminId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<string?> ResolverReporteAsync(int reporteId, string estado, int adminId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<TurnoConsultaRag>> ListarConsultasRagAsync(int limite, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<MetricasAdminDto> MetricasAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }

    [Fact]
    public async Task Validar_un_lugar_lo_marca_como_validado_a_nombre_del_administrador()
    {
        var repo = new RepositorioFalso();

        await new AdminService(repo).CambiarValidacionLugarAsync(1, new CambioValidacionLugarDto(true), Admin);

        Assert.True(repo.Validados[1]);
        Assert.Equal(Admin, repo.AdminQueValido);
    }

    [Fact]
    public async Task Se_puede_quitar_la_validacion_de_un_lugar()
    {
        var repo = new RepositorioFalso();

        await new AdminService(repo).CambiarValidacionLugarAsync(2, new CambioValidacionLugarDto(false), Admin);

        Assert.False(repo.Validados[2]);
    }

    [Fact]
    public async Task Un_lugar_que_no_existe_responde_no_encontrado()
    {
        var error = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => new AdminService(new RepositorioFalso()).CambiarValidacionLugarAsync(99, new CambioValidacionLugarDto(true), Admin));

        Assert.Equal("NO_ENCONTRADO", error.Codigo);
    }

    [Fact]
    public async Task Sin_indicar_si_esta_validado_se_rechaza_y_no_cambia_nada()
    {
        var repo = new RepositorioFalso();

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => new AdminService(repo).CambiarValidacionLugarAsync(1, new CambioValidacionLugarDto(null), Admin));

        Assert.Equal("VALIDACION", error.Codigo);
        Assert.False(repo.Validados[1]);
        Assert.Null(repo.AdminQueValido);
    }
}
