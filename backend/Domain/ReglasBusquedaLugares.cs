namespace Breaku.Domain.Rules;

/// <summary>Reglas puras de la búsqueda de lugares viables para un puente (HU6). No dependen de nada.</summary>
public static class ReglasBusquedaLugares
{
    /// <summary>Estado de un lugar verificado y aprobado por el administrador (criterio 5).</summary>
    public const string EstadoActivo = "ACTIVO";

    /// <summary>Velocidad al caminar: unos 4,8 km/h.</summary>
    public const double MetrosPorMinutoCaminando = 80;

    /// <summary>Cuántos lugares se le pasan como máximo al asistente de IA (HU7).</summary>
    public const int MaximoCandidatosAsistente = 10;

    /// <summary>Cuántos lugares se listan como máximo en la consulta directa de HU6.</summary>
    public const int MaximoLugaresListado = 50;

    /// <summary>Texto fijo cuando no hay lugares que cumplan los criterios (criterio 4).</summary>
    public const string MensajeSinResultados = ReglasRecomendacion.MensajeSinResultados;

    /// <summary>Texto cuando el estudiante todavía no eligió ni compartió su campus (HU5).</summary>
    public const string MensajeSinZona = "Elige tu campus para ver lugares cercanos.";

    /// <summary>Distancia en línea recta entre dos puntos, en metros (fórmula de haversine).</summary>
    public static double DistanciaMetros(decimal latitudA, decimal longitudA, decimal latitudB, decimal longitudB)
    {
        const double RadioTierraMetros = 6_371_000;
        static double Radianes(decimal grados) => (double)grados * Math.PI / 180;

        var diferenciaLat = Radianes(latitudB - latitudA);
        var diferenciaLong = Radianes(longitudB - longitudA);
        var a = Math.Pow(Math.Sin(diferenciaLat / 2), 2)
            + Math.Cos(Radianes(latitudA)) * Math.Cos(Radianes(latitudB)) * Math.Pow(Math.Sin(diferenciaLong / 2), 2);
        a = Math.Clamp(a, 0, 1);
        return RadioTierraMetros * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    /// <summary>Minutos de traslado de ida y vuelta caminando, redondeados hacia arriba (criterio 3).</summary>
    public static int TiempoTrasladoIdaVueltaMin(int distanciaM)
        => (int)Math.Ceiling(2 * distanciaM / MetrosPorMinutoCaminando);

    /// <summary>El lugar está dentro del radio de la zona (criterios 1 y 2). El radio justo cuenta como dentro.</summary>
    public static bool EstaDentroDelRadio(int distanciaM, int radioMaxM) => distanciaM <= radioMaxM;

    /// <summary>
    /// Traslado de ida y vuelta más la actividad caben en el puente (criterio 3).
    /// Si suman exactamente la duración del puente, sí caben.
    /// </summary>
    public static bool CabeEnElPuente(int trasladoIdaVueltaMin, int duracionActividadMin, int minutosDisponibles)
        => trasladoIdaVueltaMin + duracionActividadMin <= minutosDisponibles;
}
