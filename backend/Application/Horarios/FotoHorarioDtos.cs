namespace Breaku.Application.Horarios;

/// <summary>Una clase leída de la foto del horario. Todavía no está guardada: el estudiante la revisa y la confirma.</summary>
/// <param name="Materia" example="Cálculo I">Nombre de la materia (hasta 100 caracteres).</param>
/// <param name="DiaSemana" example="1">Día de la semana: 1 = lunes … 6 = sábado.</param>
/// <param name="HoraInicio" example="08:00">Hora de inicio (<c>HH:mm</c>, 24 h).</param>
/// <param name="HoraFin" example="09:30">Hora de fin (<c>HH:mm</c>, 24 h).</param>
/// <param name="Aula" example="A-101">Aula si se pudo leer (hasta 30 caracteres), o <c>null</c>.</param>
public record ClaseLeidaDto(string Materia, int DiaSemana, string HoraInicio, string HoraFin, string? Aula);

/// <summary>Resultado de leer la foto del horario.</summary>
/// <param name="Clases">Clases que pasaron las mismas reglas que un alta manual, ordenadas por día y hora. Incluye las que coinciden con otra clase: el estudiante elige después a cuáles asiste.</param>
/// <param name="Descartadas" example="1">Cuántas devolvió el modelo pero no cumplían las reglas (día, horas o materia inválidos, o repetidas).</param>
public record LecturaFotoHorarioDto(List<ClaseLeidaDto> Clases, int Descartadas);

/// <summary>Bloques confirmados para guardar de una vez (hasta 40).</summary>
public record GuardarLoteRequest(List<GuardarHorarioRequest> Bloques);

/// <summary>Un bloque del lote que no se guardó.</summary>
/// <param name="Indice" example="2">Posición del bloque en la lista enviada (desde 0).</param>
/// <param name="Materia" example="Física II">Materia del bloque.</param>
/// <param name="Motivo" example="Ya registraste «Cálculo I» a esa hora ese día.">Por qué no se guardó.</param>
public record BloqueRechazadoDto(int Indice, string Materia, string Motivo);

/// <summary>Resultado de guardar un lote: los bloques creados (origen <c>FOTO</c>) y los rechazados.</summary>
public record ResultadoLoteDto(List<HorarioDto> Guardados, List<BloqueRechazadoDto> Rechazados);
