using System.Text.RegularExpressions;

namespace Breaku.Domain.Rules;

/// <summary>Guardrails de entrada para el asistente de recomendaciones (HU17).</summary>
public static partial class ReglasSeguridadIa
{
    private static readonly string[] SolicitudesFueraDeAlcance =
    [
        "contraseña", "password", "token", "clave api", "código fuente",
        "información personal", "datos personales", "instrucciones del sistema"
    ];

    /// <summary>
    /// Detecta intentos comunes de cambiar las instrucciones del asistente o extraer su contexto.
    /// La validación no reemplaza la autorización ni la validación de la respuesta del modelo.
    /// </summary>
    public static string? MotivoRechazo(string? entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada)) return null;

        var texto = entrada.Trim();
        if (PatronInyeccion().IsMatch(texto))
            return "La solicitud intenta modificar las instrucciones del asistente.";

        if (SolicitudesFueraDeAlcance.Any(p => texto.Contains(p, StringComparison.OrdinalIgnoreCase)))
            return "Solo puedo recomendar actividades y lugares del campus para tu puente.";

        return null;
    }

    [GeneratedRegex(
        @"\b(ignore|ignora|olvida|omite|desobedece|cambia|reemplaza|revela|muestra|explica)\b.{0,80}\b(previous|prior|anteriores?|sistema|system|developer|desarrollador|prompt|instrucciones|reglas)\b|(\bactúa como\b|\bactua como\b|\bjailbreak\b|\bdo anything now\b|\bDAN\b)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex PatronInyeccion();
}
