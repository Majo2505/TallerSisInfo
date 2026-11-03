using Breaku.Domain;

namespace Breaku.Application.Recomendaciones;

public interface IRecomendacionRepository
{
    Task GuardarAsync(LogAuditoriaRag log, IReadOnlyList<Recomendacion> recomendaciones, CancellationToken ct = default);

    Task<Recomendacion?> ObtenerDeUsuarioAsync(long recomendacionId, int usuarioId, CancellationToken ct = default);

    Task GuardarFeedbackAsync(Recomendacion recomendacion, string feedback, CancellationToken ct = default);
}
