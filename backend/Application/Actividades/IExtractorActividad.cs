using Breaku.Application.Recomendaciones;
using Breaku.Domain.Rules;

namespace Breaku.Application.Actividades;

public interface IExtractorActividad
{
    Task<RespuestaExtractor> ExtraerAsync(PromptRag prompt, string texto, CancellationToken ct = default);
}

public record RespuestaExtractor(string Modelo, string TextoCrudo, ArgumentosExtraidos Argumentos);
