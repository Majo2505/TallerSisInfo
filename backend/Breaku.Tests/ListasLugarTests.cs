using Breaku.Domain.Rules;

namespace Breaku.Tests;

public class ListasLugarTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sin_texto_las_listas_quedan_vacias(string? texto)
    {
        Assert.Empty(ListasLugar.SepararPorComas(texto));
        Assert.Empty(ListasLugar.LeerJson(texto));
    }

    [Fact]
    public void Separa_por_comas_y_recorta_los_espacios()
        => Assert.Equal(["solitario", "grupal"], ListasLugar.SepararPorComas("solitario, grupal"));

    [Fact]
    public void Lee_una_lista_json_con_tildes()
        => Assert.Equal(["oración", "silencio"], ListasLugar.LeerJson("[\"oración\",\"silencio\"]"));

    [Fact]
    public void Un_json_roto_no_tumba_la_busqueda()
        => Assert.Empty(ListasLugar.LeerJson("[\"oración\","));
}
