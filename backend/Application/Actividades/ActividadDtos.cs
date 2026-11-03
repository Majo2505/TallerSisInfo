namespace Breaku.Application.Actividades;

public record SolicitudExtraccionDto(string? Texto, ActividadExtraidaDto? Anterior);

public record RevisionActividadDto(string? Texto, ActividadExtraidaDto? Actividad);

public record ActividadExtraidaDto(string? Actividad, string? HoraInicio, int? DuracionMin, int? MaxParticipantes);

public record RespuestaExtraccionDto(
    long ConsultaId,
    string Estado,
    string? Mensaje,
    ActividadExtraidaDto Actividad,
    List<string> Faltantes,
    List<string> Supuestos
);
