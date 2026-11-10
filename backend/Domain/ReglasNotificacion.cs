namespace Breaku.Domain.Rules;

public static class ReglasNotificacion
{
    public static readonly TimeSpan DesfaseBolivia = TimeSpan.FromHours(-4);

    /// <summary>
    /// Un aviso se manda mientras el puente está en curso y le quedan al menos estos minutos. Antes la ventana era solo el
    /// primer minuto del puente: si ningún servidor estaba prendido justo en ese minuto, el aviso no llegaba nunca.
    /// </summary>
    public const int MinutosRestantesMinimos = 10;

    /// <summary>Hasta este retraso el aviso cuenta el puente completo; después, solo lo que queda.</summary>
    public static readonly TimeSpan Puntualidad = TimeSpan.FromMinutes(1);

    public static DateTime AHoraLocal(DateTime ahoraUtc) => ahoraUtc + DesfaseBolivia;

    public static int DiaSemana(DateOnly fecha) => (int)fecha.DayOfWeek;

    /// <summary>El puente ya empezó y todavía le quedan al menos <see cref="MinutosRestantesMinimos"/>.</summary>
    public static bool DebeAvisarse(TimeOnly inicioPuente, TimeOnly finPuente, TimeOnly ahora)
        => ahora >= inicioPuente && (finPuente - ahora).TotalMinutes >= MinutosRestantesMinimos;

    /// <summary>Desde cuándo se cuentan los minutos libres del aviso: el inicio si llega a tiempo, o ahora si llega tarde.</summary>
    public static TimeOnly DesdeCuandoContar(TimeOnly inicioPuente, TimeOnly ahora)
        => ahora - inicioPuente < Puntualidad ? inicioPuente : new TimeOnly(ahora.Hour, ahora.Minute);

    public static int DuracionMinutos(TimeOnly inicio, TimeOnly fin) => (int)(fin - inicio).TotalMinutes;

    public static string Mensaje(TimeOnly inicio, TimeOnly fin)
        => $"¡Puente disponible! Tienes {FormatearDuracion(DuracionMinutos(inicio, fin))} libres, de {inicio:HH\\:mm} a {fin:HH\\:mm}.";

    /// <summary>Aviso que llega con el puente ya empezado: dice cuánto queda, no cuánto duraba.</summary>
    public static string MensajeEnCurso(TimeOnly ahora, TimeOnly fin)
        => $"¡Estás en un puente! Te quedan {FormatearDuracion(DuracionMinutos(ahora, fin))} libres, hasta las {fin:HH\\:mm}.";

    public static string MensajeDespuesDeClases(TimeOnly inicio, TimeOnly fin)
        => $"¡Terminaste tus clases! Tienes {FormatearDuracion(DuracionMinutos(inicio, fin))} libres, hasta las {fin:HH\\:mm}.";

    private static string FormatearDuracion(int minutos)
    {
        var (horas, resto) = (minutos / 60, minutos % 60);
        if (horas == 0) return $"{resto} min";
        return resto == 0 ? $"{horas} h" : $"{horas} h {resto} min";
    }
}
