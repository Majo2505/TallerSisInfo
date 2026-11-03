namespace Breaku.Domain.Rules;

public static class ReglasRecomendacion
{
    public const string MensajeSinResultados = "No encontramos opciones cercanas para este tiempo disponible.";

    public const int MaximoRecomendaciones = 3;

    public const int MaximoLargoEntrada = 300;
    public const int MaximoLargoExplicacion = 280;

    public static List<(int LugarId, string Explicacion)> FiltrarSoloRecuperados(
        IEnumerable<(int LugarId, string Explicacion)> sugerencias,
        IReadOnlyDictionary<int, string> recuperados)
    {
        var validas = new List<(int, string)>();
        var vistos = new HashSet<int>();

        foreach (var (lugarId, explicacion) in sugerencias)
        {
            if (!recuperados.TryGetValue(lugarId, out var nombre)) continue;
            if (!ExplicacionEsSegura(explicacion, nombre)) continue;
            if (!vistos.Add(lugarId)) continue;

            validas.Add((lugarId, explicacion.Trim()));
            if (validas.Count == MaximoRecomendaciones) break;
        }

        return validas;
    }

    public static string MotivoDescarte(IEnumerable<(int LugarId, string Explicacion)> sugerencias, IReadOnlyDictionary<int, string> recuperados)
        => sugerencias
               .Where(s => recuperados.ContainsKey(s.LugarId))
               .Select(s => ReglasSeguridadIa.Evaluar(s.Explicacion)?.Categoria)
               .FirstOrDefault(c => c is not null)
           ?? ReglasSeguridadIa.CategoriaFueraDeContexto;

    public static bool ExplicacionEsSegura(string? explicacion, string nombreLugar)
    {
        if (string.IsNullOrWhiteSpace(explicacion)) return false;

        var texto = explicacion.Trim();
        if (texto.Length > MaximoLargoExplicacion) return false;
        if (ReglasSeguridadIa.Evaluar(texto) is not null) return false;
        if (texto.Contains("http", StringComparison.OrdinalIgnoreCase) || texto.Contains("www.", StringComparison.OrdinalIgnoreCase)) return false;

        var sinNombre = texto.Replace(nombreLugar, "", StringComparison.OrdinalIgnoreCase);
        return !sinNombre.Any(char.IsDigit);
    }
}
