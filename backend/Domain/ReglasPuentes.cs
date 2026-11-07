namespace Breaku.Domain.Rules;

public static class ReglasPuente
{
    public const int MinutosMinimos = 45;

    public static bool EsPuente(TimeOnly finClase, TimeOnly inicioSiguiente, int minutosMinimos = MinutosMinimos)
        => (inicioSiguiente.ToTimeSpan() - finClase.ToTimeSpan()).TotalMinutes >= minutosMinimos;

    public static bool QuedaTiempoDespuesDeClases(TimeOnly finUltimaClase)
        => EsPuente(finUltimaClase, ReglasHorario.FinMasTarde);
}