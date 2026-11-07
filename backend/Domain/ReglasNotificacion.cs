namespace Breaku.Domain.Rules;

public static class ReglasNotificacion
{
    public static readonly TimeSpan DesfaseBolivia = TimeSpan.FromHours(-4);

    public static readonly TimeSpan VentanaEnvio = TimeSpan.FromMinutes(1);

    public static DateTime AHoraLocal(DateTime ahoraUtc) => ahoraUtc + DesfaseBolivia;

    public static int DiaSemana(DateOnly fecha) => (int)fecha.DayOfWeek;

    public static bool DebeAvisarse(TimeOnly inicioPuente, TimeOnly ahora)
    {
        var transcurrido = ahora.ToTimeSpan() - inicioPuente.ToTimeSpan();
        return transcurrido >= TimeSpan.Zero && transcurrido < VentanaEnvio;
    }

    public static int DuracionMinutos(TimeOnly inicio, TimeOnly fin) => (int)(fin - inicio).TotalMinutes;

    public static string Mensaje(TimeOnly inicio, TimeOnly fin)
        => $"¡Puente disponible! Tienes {FormatearDuracion(DuracionMinutos(inicio, fin))} libres, de {inicio:HH\\:mm} a {fin:HH\\:mm}.";

    public static string MensajeDespuesDeClases(TimeOnly inicio, TimeOnly fin)
        => $"¡Terminaste tus clases! Tienes {FormatearDuracion(DuracionMinutos(inicio, fin))} libres, hasta las {fin:HH\\:mm}.";

    private static string FormatearDuracion(int minutos)
    {
        var (horas, resto) = (minutos / 60, minutos % 60);
        if (horas == 0) return $"{resto} min";
        return resto == 0 ? $"{horas} h" : $"{horas} h {resto} min";
    }
}
