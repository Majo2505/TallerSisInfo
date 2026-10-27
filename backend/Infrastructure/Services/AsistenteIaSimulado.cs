using System.Text.Json;
using Breaku.Application.Recomendaciones;
using Breaku.Domain.Rules;

namespace Breaku.Infrastructure.Services;

/// <summary>
/// Asistente sin LLM: se usa cuando no hay LLM_API_KEY (ver DependencyInjection). Recomienda los primeros lugares
/// del contexto con una explicación fija, así HU7 se puede probar sin clave y sin costo.
/// </summary>
public class AsistenteIaSimulado : IAsistenteIa
{
    public const string NombreModelo = "simulado";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping   // tildes legibles en el log
    };

    public Task<RespuestaAsistente> GenerarAsync(PromptRag prompt, CancellationToken ct = default)
    {
        // Lee los lugares del mismo mensaje que recibiría el LLM: así el simulado también depende solo del contexto.
        var json = prompt.Usuario[(prompt.Usuario.IndexOf("LUGARES_DEL_CATALOGO:", StringComparison.Ordinal) + "LUGARES_DEL_CATALOGO:".Length)..];
        var lugares = JsonSerializer.Deserialize<List<LugarDelPrompt>>(json, Json) ?? [];

        var sugerencias = lugares
            .Take(ReglasRecomendacion.MaximoRecomendaciones)
            .Select(l => new SugerenciaAsistente(l.LugarId,
                $"Es un lugar de {l.Categoria.ToLowerInvariant()} verificado en el catálogo y alcanza para tu tiempo libre."))
            .ToList();

        var crudo = JsonSerializer.Serialize(new { recomendaciones = sugerencias }, Json);
        return Task.FromResult(new RespuestaAsistente(NombreModelo, crudo, sugerencias));
    }

    private sealed record LugarDelPrompt(int LugarId, string Categoria);
}
