using Breaku.Domain;

namespace Breaku.Tests;

public class DisponibilidadTests
{
    [Theory]
    [InlineData(10, 59, false)]
    [InlineData(11, 0, true)]
    [InlineData(18, 30, true)]
    [InlineData(0, 59, true)]
    [InlineData(1, 0, false)]
    [InlineData(5, 0, false)]
    public void La_ventana_operativa_es_de_7_a_21_hora_de_Bolivia(int horaUtc, int minuto, bool esperado)
        => Assert.Equal(esperado, ReglasDisponibilidad.EnHorarioOperativo(new DateTime(2026, 11, 5, horaUtc, minuto, 0, DateTimeKind.Utc)));
}
