namespace Breaku.Domain.Rules;

/// <summary>Reglas puras del asistente de contexto estricto (HU7). No dependen de nada.</summary>
public static class ReglasRecomendacion
{
    /// <summary>Texto fijo cuando no hay lugares que cumplan los criterios (criterio 4; mismo texto que HU6).</summary>
    public const string MensajeSinResultados = "No encontramos opciones cercanas para este tiempo disponible.";

    /// <summary>Cuántas recomendaciones se muestran como máximo por consulta.</summary>
    public const int MaximoRecomendaciones = 3;

    public const int MaximoLargoEntrada = 300;
    public const int MaximoLargoExplicacion = 280;

    /// <summary>
    /// Se queda solo con las sugerencias del asistente que se pueden mostrar (criterios 1 y 2):
    /// el lugar debe estar entre los recuperados, sin repetir, y la explicación debe ser segura.
    /// Respeta el orden en que las devolvió el asistente y corta en <see cref="MaximoRecomendaciones"/>.
    /// </summary>
    /// <param name="sugerencias">Pares (lugarId, explicación) tal como los devolvió el asistente.</param>
    /// <param name="recuperados">lugarId → nombre de los lugares recuperados del catálogo en esta consulta.</param>
    public static List<(int LugarId, string Explicacion)> FiltrarSoloRecuperados(
        IEnumerable<(int LugarId, string Explicacion)> sugerencias,
        IReadOnlyDictionary<int, string> recuperados)
    {
        var validas = new List<(int, string)>();
        var vistos = new HashSet<int>();

        foreach (var (lugarId, explicacion) in sugerencias)
        {
            if (!recuperados.TryGetValue(lugarId, out var nombre)) continue;   // lugar inventado o fuera del contexto
            if (!ExplicacionEsSegura(explicacion, nombre)) continue;
            if (!vistos.Add(lugarId)) continue;                                // repetido (uq_reco_log_lugar)

            validas.Add((lugarId, explicacion.Trim()));
            if (validas.Count == MaximoRecomendaciones) break;
        }

        return validas;
    }

    /// <summary>
    /// La explicación solo puede ser texto libre sobre el lugar. Distancia, tiempo, categoría y horarios los muestra
    /// el sistema desde el catálogo, así que una explicación con cifras o enlaces trae datos que no salen del
    /// contexto y se descarta. Los dígitos del propio nombre del lugar (p. ej. "Sala 2") sí se permiten.
    /// </summary>
    public static bool ExplicacionEsSegura(string? explicacion, string nombreLugar)
    {
        if (string.IsNullOrWhiteSpace(explicacion)) return false;

        var texto = explicacion.Trim();
        if (texto.Length > MaximoLargoExplicacion) return false;
        if (texto.Contains("http", StringComparison.OrdinalIgnoreCase) || texto.Contains("www.", StringComparison.OrdinalIgnoreCase)) return false;

        var sinNombre = texto.Replace(nombreLugar, "", StringComparison.OrdinalIgnoreCase);
        return !sinNombre.Any(char.IsDigit);
    }
}
