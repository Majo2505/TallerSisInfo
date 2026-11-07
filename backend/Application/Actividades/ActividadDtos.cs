namespace Breaku.Application.Actividades;

/// <param name="ConsultaAnteriorId">El <c>consultaId</c> de la respuesta anterior cuando el estudiante contesta una pregunta del
/// asistente: así la Auditoría RAG muestra los dos turnos como una sola consulta.</param>
public record SolicitudExtraccionDto(string? Texto, ActividadExtraidaDto? Anterior, long? ConsultaAnteriorId = null);

public record RevisionActividadDto(string? Texto, ActividadExtraidaDto? Actividad, long? ConsultaAnteriorId = null);

public record ActividadExtraidaDto(string? Actividad, string? HoraInicio, int? DuracionMin, int? MaxParticipantes);

public record RespuestaExtraccionDto(
    long ConsultaId,
    string Estado,
    string? Mensaje,
    ActividadExtraidaDto Actividad,
    List<string> Faltantes,
    List<string> Supuestos
);
