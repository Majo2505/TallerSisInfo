using Breaku.Domain.Rules;

namespace Breaku.Tests;

public class RangoDelHorarioTests
{
    private static string? Validar(string inicio, string fin)
        => ReglasHorario.ValidarBloque("Cálculo II", 1, TimeOnly.Parse(inicio), TimeOnly.Parse(fin));

    [Fact]
    public void Una_clase_dentro_del_horario_de_la_UCB_es_valida()
        => Assert.Null(Validar("09:00", "10:30"));

    [Fact]
    public void Empezar_justo_a_las_07_15_es_valido()
        => Assert.Null(Validar("07:15", "08:45"));

    [Fact]
    public void Terminar_justo_a_las_21_00_es_valido()
        => Assert.Null(Validar("19:30", "21:00"));

    [Theory]
    [InlineData("07:14", "08:45")]
    [InlineData("07:00", "08:30")]
    [InlineData("06:30", "07:30")]
    public void Empezar_antes_de_las_07_15_se_rechaza(string inicio, string fin)
        => Assert.Equal("Las clases empiezan desde las 07:15.", Validar(inicio, fin));

    [Theory]
    [InlineData("19:30", "21:01")]
    [InlineData("20:00", "21:30")]
    [InlineData("20:30", "22:00")]
    public void Terminar_despues_de_las_21_00_se_rechaza(string inicio, string fin)
        => Assert.Equal("Las clases terminan, como muy tarde, a las 21:00.", Validar(inicio, fin));

    [Fact]
    public void El_fin_sigue_teniendo_que_ser_posterior_al_inicio()
        => Assert.Equal("La hora de fin tiene que ser posterior a la hora de inicio.", Validar("10:00", "09:00"));
}
