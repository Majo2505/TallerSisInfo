using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Breaku.Application.Recomendaciones;
using Microsoft.Extensions.Logging;

namespace Breaku.Infrastructure.Services;

/// <summary>
/// HU7: asistente con un LLM que exponga la API de Chat Completions de OpenAI (Gemini, Groq, Ollama, OpenAI…),
/// con salida en JSON según esquema. El esquema obliga al modelo a responder solo con pares (lugarId, explicacion);
/// que esos lugares sean del contexto lo verifica después ReglasRecomendacion.
/// Al LLM solo se envía el prompt (catálogo recuperado y preferencia): nunca la ubicación ni el id del estudiante.
/// </summary>
public class AsistenteLlm : IAsistenteIa
{
    public const string NombreCliente = "Llm";
    public const int SegundosMaximosPorDefecto = 20;   // se cambia con LLM_TIMEOUT_SECONDS (un LLM local en CPU necesita más)

    private const int LargoColumnaModelo = 40;   // log_auditoria_rag.modelo es VARCHAR(40)

    // Mismo formato que pide PromptRecomendacion.Sistema. strict = true exige additionalProperties = false y que cada campo esté en required.
    private const string EsquemaRespuesta = """
        {
          "type": "object",
          "properties": {
            "recomendaciones": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "lugarId": { "type": "integer" },
                  "explicacion": { "type": "string" }
                },
                "required": ["lugarId", "explicacion"],
                "additionalProperties": false
              }
            }
          },
          "required": ["recomendaciones"],
          "additionalProperties": false
        }
        """;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly string _modelo;
    private readonly ILogger<AsistenteLlm> _logger;

    public AsistenteLlm(HttpClient http, string modelo, ILogger<AsistenteLlm> logger)
    {
        _http = http;
        _modelo = modelo;
        _logger = logger;
    }

    public async Task<RespuestaAsistente> GenerarAsync(PromptRag prompt, CancellationToken ct = default)
    {
        var cuerpo = new JsonObject
        {
            ["model"] = _modelo,
            ["messages"] = new JsonArray
            {
                new JsonObject { ["role"] = "system", ["content"] = prompt.Sistema },
                new JsonObject { ["role"] = "user", ["content"] = prompt.Usuario }
            },
            ["response_format"] = new JsonObject
            {
                ["type"] = "json_schema",
                ["json_schema"] = new JsonObject
                {
                    ["name"] = "recomendaciones",
                    ["strict"] = true,
                    ["schema"] = JsonNode.Parse(EsquemaRespuesta)
                }
            }
        };

        using var respuesta = await _http.PostAsJsonAsync("chat/completions", cuerpo, ct);
        if (!respuesta.IsSuccessStatusCode)
        {
            // El cuerpo del error no incluye la clave; sirve para saber si es cuota, modelo inválido, etc.
            _logger.LogWarning("El LLM respondió {Estado}: {Cuerpo}", (int)respuesta.StatusCode, await respuesta.Content.ReadAsStringAsync(ct));
            throw new HttpRequestException($"El LLM respondió {(int)respuesta.StatusCode}.");
        }

        using var doc = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var opcion = doc.RootElement.GetProperty("choices")[0];
        var mensaje = opcion.GetProperty("message");

        if (mensaje.TryGetProperty("refusal", out var rechazo) && rechazo.ValueKind == JsonValueKind.String)
            throw new InvalidOperationException($"El modelo se negó a responder: {rechazo.GetString()}");
        if (opcion.GetProperty("finish_reason").GetString() != "stop")
            throw new InvalidOperationException($"Respuesta incompleta del LLM ({opcion.GetProperty("finish_reason").GetString()}).");

        var contenido = mensaje.GetProperty("content").GetString()
                        ?? throw new InvalidOperationException("El LLM respondió sin contenido.");
        var leida = JsonSerializer.Deserialize<RespuestaJson>(contenido, Json)
                    ?? throw new InvalidOperationException("La respuesta del LLM no es el JSON esperado.");

        var modelo = doc.RootElement.TryGetProperty("model", out var m) ? m.GetString() ?? _modelo : _modelo;
        var sugerencias = (leida.Recomendaciones ?? [])
            .Select(r => new SugerenciaAsistente(r.LugarId, r.Explicacion ?? ""))
            .ToList();

        return new RespuestaAsistente(modelo.Length > LargoColumnaModelo ? modelo[..LargoColumnaModelo] : modelo, contenido, sugerencias);
    }

    private sealed record RespuestaJson(List<ItemJson>? Recomendaciones);
    private sealed record ItemJson(int LugarId, string? Explicacion);
}
