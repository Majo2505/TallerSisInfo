using Breaku.Domain;

namespace Breaku.Application.Recomendaciones;

/// <summary>Puerto: lo implementa Infrastructure.</summary>
public interface IRecomendacionRepository
{
    /// <summary>
    /// Guarda en una sola transacción el log, sus lugares recuperados y las recomendaciones mostradas (criterio 3).
    /// Al volver, <c>log.Id</c> y el <c>Id</c> de cada recomendación ya están asignados.
    /// </summary>
    Task GuardarAsync(LogAuditoriaRag log, IReadOnlyList<Recomendacion> recomendaciones, CancellationToken ct = default);

    Task<Recomendacion?> ObtenerDeUsuarioAsync(long recomendacionId, int usuarioId, CancellationToken ct = default);

    Task GuardarFeedbackAsync(Recomendacion recomendacion, string feedback, CancellationToken ct = default);
}
