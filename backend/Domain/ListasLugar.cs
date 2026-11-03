using System.Text.Json;

namespace Breaku.Domain.Rules;

public static class ListasLugar
{
    public static List<string> SepararPorComas(string? texto)
        => string.IsNullOrWhiteSpace(texto)
            ? []
            : texto.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    public static List<string> LeerJson(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(texto) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
