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
}
