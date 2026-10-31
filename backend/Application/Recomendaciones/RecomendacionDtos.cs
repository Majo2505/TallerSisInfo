namespace Breaku.Application.Recomendaciones;

/// <summary>Pedido de recomendación para un puente del usuario.</summary>
/// <param name="PuenteId" example="5">Puente del usuario para el que se busca qué hacer (ver <c>GET /api/puentes</c>).</param>
/// <param name="Entrada" example="Estoy cansado, quiero algo tranquilo">Preferencia libre del estudiante (opcional, máximo 300 caracteres).</param>
public record SolicitudRecomendacionDto(int PuenteId, string? Entrada);

/// <summary>Resultado de la consulta al asistente.</summary>
/// <param name="ConsultaId" example="42">Identificador del registro de auditoría de esta consulta.</param>
/// <param name="Estado" example="OK"><c>OK</c> = hay recomendaciones. <c>SIN_RESULTADOS</c> = no hay; se muestra <c>mensaje</c>.</param>
/// <param name="Mensaje" example="No encontramos opciones cercanas para este tiempo disponible.">Texto para mostrar cuando no hay recomendaciones. <c>null</c> si las hay.</param>
/// <param name="Recomendaciones">Recomendaciones, de la más a la menos adecuada (máximo 3). Vacía si <c>estado</c> = <c>SIN_RESULTADOS</c>.</param>
public record RespuestaRecomendacionDto(
    long ConsultaId,
    string Estado,
    string? Mensaje,
    List<RecomendacionDto> Recomendaciones
);

/// <summary>
/// Una recomendación. Todos los datos salen del catálogo; del asistente solo sale <c>explicacion</c>.
/// </summary>
/// <param name="Id" example="87">Identificador de la recomendación.</param>
/// <param name="LugarId" example="2">Lugar del catálogo que respalda la recomendación.</param>
/// <param name="Nombre" example="Biblioteca Central">Nombre del lugar, tal como está en el catálogo.</param>
/// <param name="Categoria" example="Estudio">Categoría del lugar.</param>
/// <param name="DistanciaM" example="200">Distancia desde la zona del estudiante, en metros.</param>
/// <param name="TiempoTrasladoMin" example="6">Tiempo de traslado de ida y vuelta, en minutos.</param>
/// <param name="DuracionEstimadaMin" example="30">Tiempo estimado de la actividad en el lugar, en minutos.</param>
/// <param name="Explicacion" example="Ideal para repasar en silencio antes de tu próxima clase.">Por qué encaja, redactado por el asistente.</param>
/// <param name="Feedback" example="LIKE">Valoración del estudiante: <c>LIKE</c>, <c>DISLIKE</c> o <c>null</c> si aún no valoró.</param>
public record RecomendacionDto(
    long Id,
    int LugarId,
    string Nombre,
    string Categoria,
    int DistanciaM,
    int TiempoTrasladoMin,
    int DuracionEstimadaMin,
    string Explicacion,
    string? Feedback = null
);

public record FeedbackRecomendacionDto(string Feedback);
