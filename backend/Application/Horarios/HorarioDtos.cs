namespace Breaku.Application.Horarios;

/// <summary>Datos de un bloque de clase. Sirve para crear (POST) y para reemplazar (PUT) un bloque.</summary>
/// <param name="Materia" example="Cálculo I">Nombre de la materia. Obligatorio, hasta 100 caracteres (se recortan los espacios de los extremos).</param>
/// <param name="DiaSemana" example="1">Día de la semana: 1 = lunes … 6 = sábado.</param>
/// <param name="HoraInicio" example="08:00">Hora de inicio en formato 24 h (<c>HH:mm</c> o <c>HH:mm:ss</c>).</param>
/// <param name="HoraFin" example="09:30">Hora de fin en formato 24 h (<c>HH:mm</c> o <c>HH:mm:ss</c>). Debe ser posterior a la hora de inicio.</param>
/// <param name="Aula" example="A-101">Aula (opcional, hasta 30 caracteres). Si llega vacía se guarda como <c>null</c>.</param>
public record GuardarHorarioRequest(string Materia, int DiaSemana, string HoraInicio, string HoraFin, string? Aula);

/// <summary>Bloque de clase del estudiante.</summary>
/// <param name="Id" example="12">Identificador del bloque.</param>
/// <param name="Materia" example="Cálculo I">Nombre de la materia.</param>
/// <param name="DiaSemana" example="1">Día de la semana: 1 = lunes … 6 = sábado.</param>
/// <param name="HoraInicio" example="08:00:00">Hora de inicio (<c>HH:mm:ss</c>, 24 h).</param>
/// <param name="HoraFin" example="09:30:00">Hora de fin (<c>HH:mm:ss</c>, 24 h).</param>
/// <param name="Aula" example="A-101">Aula, o <c>null</c> si no se indicó.</param>
/// <param name="Origen" example="MANUAL">Cómo se cargó el bloque: <c>MANUAL</c> (hoy siempre) o <c>FOTO</c> (reservado). El cliente no lo envía.</param>
public record HorarioDto(int Id, string Materia, int DiaSemana, string HoraInicio, string HoraFin, string? Aula, string Origen);

/// <summary>Error de negocio con un código que la Api traduce a HTTP.</summary>
public class ReglaNegocioException : Exception
{
    public string Codigo { get; }   // VALIDACION | SOLAPAMIENTO | NO_ENCONTRADO
    public ReglaNegocioException(string codigo, string mensaje) : base(mensaje) => Codigo = codigo;
}
