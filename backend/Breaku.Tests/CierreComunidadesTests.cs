using Breaku.Application.Comunidades;

namespace Breaku.Tests;

public class CierreComunidadesTests
{
    [Fact]
    public async Task Archiva_las_comunidades_vencidas_a_la_hora_indicada()
    {
        var repo = new ComunidadServiceTests.RepositorioFalso { VencidasArchivables = 2 };
        var ahora = new DateTime(2026, 11, 3, 15, 30, 0, DateTimeKind.Utc);

        var archivadas = await new CierreComunidadesService(repo).ArchivarVencidasAsync(ahora);

        Assert.Equal(2, archivadas);
        Assert.Equal(ahora, repo.UltimoCierre);
    }
}
