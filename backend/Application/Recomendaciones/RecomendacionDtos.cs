namespace Breaku.Application.Recomendaciones;

public record SolicitudRecomendacionDto(int PuenteId, string? Entrada);

public record RespuestaRecomendacionDto(
    long ConsultaId,
    string Estado,
    string? Mensaje,
    List<RecomendacionDto> Recomendaciones
);

public record RecomendacionDto(
    long Id,
    int LugarId,
    string Nombre,
    string Categoria,
    int DistanciaM,
    int TiempoTrasladoMin,
    int DuracionEstimadaMin,
    string Explicacion,
    string? Feedback = null,
    string? FotoUrl = null
);

public record FeedbackRecomendacionDto(string? Feedback);

public record FeedbackGuardadoDto(long RecomendacionId, string Feedback, DateTime FechaFeedback);
