namespace Breaku.Domain.Rules;

public static class ReglasFeedback
{
    public const string MeGusta = "LIKE";
    public const string NoMeSirve = "DISLIKE";

    public const string MensajeInvalido = "Tu valoración tiene que ser LIKE o DISLIKE.";

    public static string? Normalizar(string? voto)
    {
        var valor = voto?.Trim().ToUpperInvariant();
        return valor is MeGusta or NoMeSirve ? valor : null;
    }

    public static bool CambiaElVoto(string? actual, string nuevo) => actual != nuevo;

    /// <summary>
    /// La puntuación de un lugar, de 1 a 5 con un decimal, a partir de los votos de los estudiantes («me gusta» y
    /// «no me sirve» en sus recomendaciones). No hay estrellas inventadas: sale de esos votos.
    /// Se suaviza con un voto imaginario de cada lado ((likes + 1) / (votos + 2)) para que un solo «me gusta» no dé 5,0
    /// y un solo «no me sirve», 1,0. Sin votos, no hay puntuación (null).
    /// </summary>
    public static decimal? Puntuacion(int meGusta, int noMeSirve)
    {
        if (meGusta < 0 || noMeSirve < 0) throw new ArgumentOutOfRangeException(nameof(meGusta), "Los votos no pueden ser negativos.");
        var votos = meGusta + noMeSirve;
        if (votos == 0) return null;
        var proporcion = (meGusta + 1m) / (votos + 2m);
        return Math.Round(1m + 4m * proporcion, 1, MidpointRounding.AwayFromZero);
    }
}
