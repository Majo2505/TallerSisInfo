using Breaku.Domain.Rules;

namespace Breaku.Tests;

public class ValoracionDeRecomendacionesTests
{
    [Theory]
    [InlineData("LIKE", "LIKE")]
    [InlineData("DISLIKE", "DISLIKE")]
    [InlineData("like", "LIKE")]
    [InlineData("  Dislike ", "DISLIKE")]
    public void Acepta_me_gusta_y_no_me_sirve_sin_importar_mayusculas_ni_espacios(string voto, string esperado)
        => Assert.Equal(esperado, ReglasFeedback.Normalizar(voto));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("MEH")]
    [InlineData("LIKES")]
    [InlineData("👍")]
    public void Rechaza_cualquier_otro_valor(string? voto)
        => Assert.Null(ReglasFeedback.Normalizar(voto));

    [Fact]
    public void El_primer_voto_se_guarda()
        => Assert.True(ReglasFeedback.CambiaElVoto(null, ReglasFeedback.MeGusta));

    [Fact]
    public void Cambiar_de_opinion_reemplaza_el_voto()
        => Assert.True(ReglasFeedback.CambiaElVoto(ReglasFeedback.MeGusta, ReglasFeedback.NoMeSirve));

    [Fact]
    public void Repetir_el_mismo_voto_no_escribe_nada()
        => Assert.False(ReglasFeedback.CambiaElVoto(ReglasFeedback.NoMeSirve, ReglasFeedback.NoMeSirve));
}
