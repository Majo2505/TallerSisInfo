namespace Breaku.Domain.Rules;

/// <summary>
/// Reglas puras de la valoración de recomendaciones (HU9). No dependen de nada.
/// Hay una sola valoración por recomendación: votar de nuevo reemplaza la anterior, nunca se duplica.
/// </summary>
public static class ReglasFeedback
{
    public const string MeGusta = "LIKE";
    public const string NoMeSirve = "DISLIKE";

    public const string MensajeInvalido = "Tu valoración tiene que ser LIKE o DISLIKE.";

    /// <summary>
    /// Devuelve el voto normalizado (<c>LIKE</c> o <c>DISLIKE</c>), sin importar mayúsculas ni espacios,
    /// o <c>null</c> si no es un voto válido.
    /// </summary>
    public static string? Normalizar(string? voto)
    {
        var valor = voto?.Trim().ToUpperInvariant();
        return valor is MeGusta or NoMeSirve ? valor : null;
    }

    /// <summary>Si hay que escribir en la base: solo cuando el voto cambia. Repetir el mismo voto no hace nada.</summary>
    public static bool CambiaElVoto(string? actual, string nuevo) => actual != nuevo;
}
