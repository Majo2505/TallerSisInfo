namespace Breaku.Domain.Rules;

/// <summary>Reglas puras de los avisos de puente (HU4). No dependen de nada.</summary>
public static class ReglasNotificacion
{
    /// <summary>Bolivia es UTC-4 todo el año (sin horario de verano). Los horarios y puentes están en hora local.</summary>
    public static readonly TimeSpan DesfaseBolivia = TimeSpan.FromHours(-4);

    /// <summary>
    /// El aviso sale cuando el puente ya empezó (nunca antes) y como máximo este tiempo después.
    /// Pasada la ventana ya no se envía: un aviso tardío no sirve para "actuar a tiempo".
    /// </summary>
    public static readonly TimeSpan VentanaEnvio = TimeSpan.FromMinutes(1);

    public static DateTime AHoraLocal(DateTime ahoraUtc) => ahoraUtc + DesfaseBolivia;

    /// <summary>1 = lunes … 6 = sábado (como horario.dia_semana); el domingo devuelve 0 y no tiene puentes.</summary>
    public static int DiaSemana(DateOnly fecha) => (int)fecha.DayOfWeek;

    /// <summary>¿El puente empieza dentro de la ventana de envío que termina en <paramref name="ahora"/>?</summary>
    public static bool DebeAvisarse(TimeOnly inicioPuente, TimeOnly ahora)
    {
        var transcurrido = ahora.ToTimeSpan() - inicioPuente.ToTimeSpan();
        return transcurrido >= TimeSpan.Zero && transcurrido < VentanaEnvio;
    }

    public static int DuracionMinutos(TimeOnly inicio, TimeOnly fin) => (int)(fin - inicio).TotalMinutes;

    /// <summary>Texto del aviso, con la duración disponible (criterio 1). Cabe de sobra en los 200 caracteres de la columna.</summary>
    public static string Mensaje(TimeOnly inicio, TimeOnly fin)
        => $"¡Puente disponible! Tienes {FormatearDuracion(DuracionMinutos(inicio, fin))} libres, de {inicio:HH\\:mm} a {fin:HH\\:mm}.";

    private static string FormatearDuracion(int minutos)
    {
        var (horas, resto) = (minutos / 60, minutos % 60);
        if (horas == 0) return $"{resto} min";
        return resto == 0 ? $"{horas} h" : $"{horas} h {resto} min";
    }
}
