using Breaku.Domain;

namespace Breaku.Tests;

public class CorreoInstitucionalTests
{
    [Theory]
    [InlineData("ana.perez@ucb.edu.bo", true)]
    [InlineData("  ANA@UCB.EDU.BO ", true)]
    [InlineData("ana@gmail.com", false)]
    [InlineData("@ucb.edu.bo", false)]
    [InlineData("ana@x@ucb.edu.bo", false)]
    [InlineData("ana@ucb.edu.bo.evil.com", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Solo_acepta_correos_del_dominio_institucional(string? correo, bool esperado)
        => Assert.Equal(esperado, ReglasCorreoInstitucional.EsInstitucional(correo));
}
