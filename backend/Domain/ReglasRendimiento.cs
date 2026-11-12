namespace Breaku.Domain.Rules;

/// <summary>
/// HU19: la recomendación responde en menos de 3,5 s. A la IA se le da un presupuesto y, si no contesta a tiempo (o
/// falla), se recomiendan los lugares más cercanos de los que ya se recuperaron (HU6): el estudiante nunca espera de más
/// ni se queda sin respuesta en horas pico.
/// </summary>
public static class ReglasRendimiento
{
    public const int ObjetivoRecomendacionMs = 3500;

    /// <summary>Lo que se espera a la IA. El resto (recuperar, validar y guardar) entra en el margen hasta 3,5 s.</summary>
    public const int PresupuestoIaMs = 2800;

    /// <summary>Lo que queda en <c>log_auditoria_rag.modelo</c> cuando respondió el plan B.</summary>
    public const string ModeloRespaldo = "respaldo-sin-ia";

    /// <summary>Consultas por minuto de cada estudiante a lo que usa la IA (recomendar, extraer, revisar y leer la foto).</summary>
    public const int ConsultasIaPorMinuto = 10;
    public const string PoliticaIa = "ia";
    public const string CodigoDemasiadasConsultas = "DEMASIADAS_SOLICITUDES";
    public const string MensajeDemasiadasConsultas = "Hiciste muchas consultas seguidas. Espera un minuto y vuelve a intentarlo.";

    public const string MensajeRespaldo =
        "El asistente estaba muy solicitado, así que te mostramos las opciones más cercanas para tu puente.";

    private static readonly string[] Explicaciones =
    [
        "Es la opción más cercana: llegas rápido y te queda tiempo libre en tu puente.",
        "También queda cerca y alcanzas a ir y volver dentro de tu puente.",
        "Otra opción cercana que entra en tu tiempo libre."
    ];

    public sealed record Candidato(int LugarId, int TiempoTrasladoMin, int DistanciaM, int DuracionEstimadaMin);

    /// <summary>
    /// El plan B: primero los que entran con ida, actividad y vuelta en el puente; después, los más cercanos (por
    /// minutos de traslado y luego por metros). Sin cifras en la explicación, igual que la de la IA (la app ya las muestra).
    /// </summary>
    public static List<(int LugarId, string Explicacion)> Respaldo(IEnumerable<Candidato> candidatos, int minutosDisponibles)
        => candidatos
            .DistinctBy(c => c.LugarId)
            .OrderBy(c => EntraEnElPuente(c, minutosDisponibles) ? 0 : 1)
            .ThenBy(c => c.TiempoTrasladoMin)
            .ThenBy(c => c.DistanciaM)
            .ThenBy(c => c.LugarId)
            .Take(ReglasRecomendacion.MaximoRecomendaciones)
            .Select((c, i) => (c.LugarId, Explicaciones[i]))
            .ToList();

    public static bool EntraEnElPuente(Candidato c, int minutosDisponibles)
        => c.TiempoTrasladoMin * 2 + c.DuracionEstimadaMin <= minutosDisponibles;

    public static bool DentroDelObjetivo(int latenciaMs) => latenciaMs < ObjetivoRecomendacionMs;

    /// <summary>Percentil por el método del rango más cercano (el mismo de <c>/health/metrics</c>). Null sin datos.</summary>
    public static int? Percentil(IReadOnlyCollection<int> valores, double percentil)
    {
        if (valores.Count == 0) return null;
        var ordenados = valores.Order().ToArray();
        var indice = Math.Clamp((int)Math.Ceiling(ordenados.Length * percentil) - 1, 0, ordenados.Length - 1);
        return ordenados[indice];
    }
}
