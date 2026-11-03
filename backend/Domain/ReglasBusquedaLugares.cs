namespace Breaku.Domain.Rules;

public static class ReglasBusquedaLugares
{
    public const string EstadoActivo = "ACTIVO";

    public const double MetrosPorMinutoCaminando = 80;

    public const int MaximoCandidatosAsistente = 10;

    public const int MaximoLugaresListado = 50;

    public const string MensajeSinResultados = ReglasRecomendacion.MensajeSinResultados;

    public const string MensajeSinZona = "Elige tu campus para ver lugares cercanos.";

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

    public static int TiempoTrasladoIdaVueltaMin(int distanciaM)
        => (int)Math.Ceiling(2 * distanciaM / MetrosPorMinutoCaminando);

    public static bool EstaDentroDelRadio(int distanciaM, int radioMaxM) => distanciaM <= radioMaxM;
    
    public static bool TieneMetricasCompletas(string? nombre, string? categoria, int distanciaM, int duracionActividadMin)
        => !string.IsNullOrWhiteSpace(nombre)
           && !string.IsNullOrWhiteSpace(categoria)
           && distanciaM >= 0
           && duracionActividadMin > 0;

    public static bool CabeEnElPuente(int trasladoIdaVueltaMin, int duracionActividadMin, int minutosDisponibles)
        => trasladoIdaVueltaMin + duracionActividadMin <= minutosDisponibles;
}
