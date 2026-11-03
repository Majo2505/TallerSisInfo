using Breaku.Application.Recomendaciones;
using Breaku.Domain.Rules;

namespace Breaku.Application.Actividades;

/// <summary>
/// Puerto: el modelo que convierte el texto libre del estudiante en datos de una actividad (HU11). Lo implementa
/// Infrastructure: <c>ExtractorActividadLlm</c> (tool calling) si hay LLM_API_KEY; si no, <c>ExtractorActividadSimulado</c>.
/// </summary>
public interface IExtractorActividad
{
    /// <summary>
    /// Devuelve los argumentos con los que el modelo llamó a <c>registrar_actividad</c>, sin validar.
    /// Si el servicio falla, tarda demasiado o responde algo que no se puede leer, lanza una excepción:
    /// ExtraccionActividadService la registra como ERROR en el log.
    /// </summary>
    /// <param name="prompt">Lo que se le envía al LLM.</param>
    /// <param name="texto">El texto del estudiante ya saneado (lo usa el extractor simulado, que no lee el prompt).</param>
    /// <param name="ct">Cancelación de la petición.</param>
    Task<RespuestaExtractor> ExtraerAsync(PromptRag prompt, string texto, CancellationToken ct = default);
}

/// <param name="Modelo">Nombre del modelo que respondió (se copia al log).</param>
/// <param name="TextoCrudo">Argumentos de la llamada a la función, tal cual (se copian al log).</param>
/// <param name="Argumentos">Argumentos leídos. SIN validar: los valida ReglasExtraccion.</param>
public record RespuestaExtractor(string Modelo, string TextoCrudo, ArgumentosExtraidos Argumentos);
