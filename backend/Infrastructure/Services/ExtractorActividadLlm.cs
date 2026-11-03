using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Breaku.Application.Actividades;
using Breaku.Application.Recomendaciones;
using Breaku.Domain.Rules;
using Microsoft.Extensions.Logging;

namespace Breaku.Infrastructure.Services;

public class ExtractorActividadLlm : IExtractorActividad
{
    private const int LargoColumnaModelo = 40;

    private const string EsquemaParametros = """
        {
          "type": "object",
          "properties": {
            "entendido": { "type": "boolean", "description": "true si el texto describe alguna actividad" },
            "actividad": { "type": ["string", "null"], "description": "Qué se va a hacer, en pocas palabras y en minúsculas" },
            "hora": { "type": ["string", "null"], "description": "Hora de inicio concreta de hoy, formato 24 h HH:mm" },
            "enMinutos": { "type": ["integer", "null"], "description": "Inicio relativo: minutos desde ahora (ahora = 0)" },
            "duracionMin": { "type": ["integer", "null"], "description": "Duración en minutos" },
            "maxParticipantes": { "type": ["integer", "null"], "description": "Personas en total, contando a quien escribe" }
          },
          "required": ["entendido", "actividad", "hora", "enMinutos", "duracionMin", "maxParticipantes"],
          "additionalProperties": false
        }
        """;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly HttpClient _http;
    private readonly string _modelo;
    private readonly ILogger<ExtractorActividadLlm> _logger;

    public ExtractorActividadLlm(HttpClient http, string modelo, ILogger<ExtractorActividadLlm> logger)
    {
        _http = http;
        _modelo = modelo;
        _logger = logger;
    }

    public async Task<RespuestaExtractor> ExtraerAsync(PromptRag prompt, string texto, CancellationToken ct = default)
    {
        var cuerpo = new JsonObject
        {
            ["model"] = _modelo,
            ["messages"] = new JsonArray
            {
                new JsonObject { ["role"] = "system", ["content"] = prompt.Sistema },
                new JsonObject { ["role"] = "user", ["content"] = prompt.Usuario }
            },
            ["tools"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "function",
                    ["function"] = new JsonObject
                    {
                        ["name"] = PromptExtraccion.NombreFuncion,
                        ["description"] = "Registra los datos de la actividad que describe el estudiante.",
                        ["strict"] = true,
                        ["parameters"] = JsonNode.Parse(EsquemaParametros)
                    }
                }
            },
            ["tool_choice"] = new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject { ["name"] = PromptExtraccion.NombreFuncion }
            }
        };

        if (EsOpenAi(_http.BaseAddress)) cuerpo["reasoning_effort"] = "none";

        using var respuesta = await _http.PostAsJsonAsync("chat/completions", cuerpo, ct);
        if (!respuesta.IsSuccessStatusCode)
        {
            _logger.LogWarning("El LLM respondió {Estado}: {Cuerpo}", (int)respuesta.StatusCode, await respuesta.Content.ReadAsStringAsync(ct));
            throw new HttpRequestException($"El LLM respondió {(int)respuesta.StatusCode}.");
        }

        using var doc = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var opcion = doc.RootElement.GetProperty("choices")[0];
        var mensaje = opcion.GetProperty("message");

        if (mensaje.TryGetProperty("refusal", out var rechazo) && rechazo.ValueKind == JsonValueKind.String)
            throw new InvalidOperationException($"El modelo se negó a responder: {rechazo.GetString()}");
        if (opcion.TryGetProperty("finish_reason", out var fin) && fin.GetString() == "length")
            throw new InvalidOperationException("Respuesta incompleta del LLM (length).");

        var argumentos = LeerArgumentos(mensaje)
                         ?? throw new InvalidOperationException($"El LLM no llamó a {PromptExtraccion.NombreFuncion}.");
        var leidos = JsonSerializer.Deserialize<ArgumentosJson>(argumentos, Json)
                     ?? throw new InvalidOperationException("Los argumentos del LLM no son el JSON esperado.");

        var modelo = doc.RootElement.TryGetProperty("model", out var m) ? m.GetString() ?? _modelo : _modelo;
        var extraidos = new ArgumentosExtraidos(
            leidos.Entendido ?? !string.IsNullOrWhiteSpace(leidos.Actividad),
            leidos.Actividad, leidos.Hora, leidos.EnMinutos, leidos.DuracionMin, leidos.MaxParticipantes);

        return new RespuestaExtractor(modelo.Length > LargoColumnaModelo ? modelo[..LargoColumnaModelo] : modelo, argumentos, extraidos);
    }

    public static bool EsOpenAi(Uri? urlBase)
        => urlBase is not null && urlBase.Host.Equals("api.openai.com", StringComparison.OrdinalIgnoreCase);

    private static string? LeerArgumentos(JsonElement mensaje)
    {
        if (mensaje.TryGetProperty("tool_calls", out var llamadas) && llamadas.ValueKind == JsonValueKind.Array)
        {
            foreach (var llamada in llamadas.EnumerateArray())
            {
                if (!llamada.TryGetProperty("function", out var funcion)) continue;
                if (funcion.GetProperty("name").GetString() != PromptExtraccion.NombreFuncion) continue;

                var args = funcion.GetProperty("arguments");
                return args.ValueKind == JsonValueKind.String ? args.GetString() : args.GetRawText();
            }
        }

        if (mensaje.TryGetProperty("content", out var contenido) && contenido.ValueKind == JsonValueKind.String)
        {
            var texto = contenido.GetString()!.Trim();
            var inicio = texto.IndexOf('{');
            var final = texto.LastIndexOf('}');
            if (inicio >= 0 && final > inicio) return texto[inicio..(final + 1)];
        }

        return null;
    }

    private sealed record ArgumentosJson(
        bool? Entendido, string? Actividad, string? Hora, int? EnMinutos, int? DuracionMin, int? MaxParticipantes);
}
