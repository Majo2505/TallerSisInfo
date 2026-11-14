using Breaku.Domain.Rules;

namespace Breaku.Tests;

/// <summary>La puntuación de 1 a 5 que se muestra en el catálogo del admin y en las recomendaciones.</summary>
public class PuntuacionLugarTests
{
    [Fact]
    public void Sin_votos_no_hay_puntuacion()
        => Assert.Null(ReglasFeedback.Puntuacion(0, 0));

    [Fact]
    public void Un_solo_me_gusta_no_da_cinco()
        => Assert.Equal(3.7m, ReglasFeedback.Puntuacion(1, 0));

    [Fact]
    public void Un_solo_no_me_sirve_no_da_uno()
        => Assert.Equal(2.3m, ReglasFeedback.Puntuacion(0, 1));

    [Fact]
    public void Votos_parejos_quedan_en_el_medio()
        => Assert.Equal(3.0m, ReglasFeedback.Puntuacion(5, 5));

    [Fact]
    public void Muchos_me_gusta_se_acercan_a_cinco_sin_llegar()
    {
        var p = ReglasFeedback.Puntuacion(40, 0)!.Value;
        Assert.True(p is > 4.8m and < 5m, p.ToString());
    }

    [Theory]
    [InlineData(20, 2, 4.5)]
    [InlineData(8, 4, 3.6)]
    [InlineData(1, 9, 1.7)]
    public void Queda_siempre_entre_uno_y_cinco_con_un_decimal(int meGusta, int noMeSirve, double esperado)
    {
        var p = ReglasFeedback.Puntuacion(meGusta, noMeSirve)!.Value;
        Assert.Equal((decimal)esperado, p);
        Assert.InRange(p, 1m, 5m);
    }

    [Fact]
    public void Votos_negativos_son_un_error()
        => Assert.Throws<ArgumentOutOfRangeException>(() => ReglasFeedback.Puntuacion(-1, 0));
}
