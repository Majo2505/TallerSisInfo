namespace Breaku.Application.Horarios;

public record ClaseLeidaDto(string Materia, int DiaSemana, string HoraInicio, string HoraFin, string? Aula);

public record LecturaFotoHorarioDto(List<ClaseLeidaDto> Clases, int Descartadas);

public record GuardarLoteRequest(List<GuardarHorarioRequest> Bloques);

public record BloqueRechazadoDto(int Indice, string Materia, string Motivo);

public record ResultadoLoteDto(List<HorarioDto> Guardados, List<BloqueRechazadoDto> Rechazados);
