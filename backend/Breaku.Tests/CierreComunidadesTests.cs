using Breaku.Application.Comunidades;

namespace Breaku.Tests;

public class CierreComunidadesTests
{
    private static readonly DateTime Ahora = new(2026, 11, 3, 15, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Archiva_las_comunidades_vencidas_a_la_hora_indicada()
    {
        var repo = new ComunidadServiceTests.RepositorioFalso { VencidasArchivables = [4, 9] };

        var archivadas = await new CierreComunidadesService(repo).ArchivarVencidasAsync(Ahora);

        Assert.Equal([4, 9], archivadas);
        Assert.Equal(Ahora, repo.UltimoCierre);
    }

    [Fact]
    public async Task Al_archivar_se_apagan_los_avisos_de_esas_comunidades()
    {
        var repo = new ComunidadServiceTests.RepositorioFalso { VencidasArchivables = [4, 9] };

        await new CierreComunidadesService(repo).ArchivarVencidasAsync(Ahora);

        var apagados = Assert.Single(repo.AvisosApagados);
        Assert.Equal([4, 9], apagados!);
    }

    [Fact]
    public async Task Si_no_vencio_ninguna_no_se_tocan_los_avisos()
    {
        var repo = new ComunidadServiceTests.RepositorioFalso();

        var archivadas = await new CierreComunidadesService(repo).ArchivarVencidasAsync(Ahora);

        Assert.Empty(archivadas);
        Assert.Empty(repo.AvisosApagados);
    }

    [Fact]
    public async Task Al_arrancar_se_apagan_los_avisos_de_todas_las_archivadas()
    {
        var repo = new ComunidadServiceTests.RepositorioFalso();

        var apagados = await new CierreComunidadesService(repo).LimpiarAvisosDeArchivadasAsync();

        Assert.Equal(7, apagados);
        Assert.Null(Assert.Single(repo.AvisosApagados));
    }
}
