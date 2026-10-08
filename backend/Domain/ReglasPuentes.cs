namespace Breaku.Domain.Rules;

public static class ReglasPuente
{
    public const int MinutosMinimos = 45;

    /// <summary>
    /// Dice si el hueco entre el fin de una clase y el inicio de la siguiente es un puente.
    /// </summary>
    public static bool EsPuente(TimeOnly finClase, TimeOnly inicioSiguiente, int minutosMinimos = MinutosMinimos)
        => (inicioSiguiente.ToTimeSpan() - finClase.ToTimeSpan()).TotalMinutes >= minutosMinimos;
}