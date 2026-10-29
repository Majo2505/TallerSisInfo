namespace Breaku.Application.Horarios;

/// <summary>Datos de un bloque de clase. Sirve para crear (POST) y para reemplazar (PUT) un bloque.</summary>
/// <param name="Materia" example="Cálculo I">Nombre de la materia. Obligatorio, hasta 100 caracteres (se recortan los espacios de los extremos).</param>
/// <param name="DiaSemana" example="1">Día de la semana: 1 = lunes … 6 = sábado.</param>
/// <param name="HoraInicio" example="08:00">Hora de inicio en formato 24 h (<c>HH:mm</c> o <c>HH:mm:ss</c>).</param>
/// <param name="HoraFin" example="09:30">Hora de fin en formato 24 h (<c>HH:mm</c> o <c>HH:mm:ss</c>). Debe ser posterior a la hora de inicio.</param>
/// <param name="Aula" example="A-101">Aula (opcional, hasta 30 caracteres). Si llega vacía se guarda como <c>null</c>.</param>
/// <param name="Asiste" example="true">Si el estudiante asiste a este bloque. Si no se envía: <c>true</c> al crear y se conserva el valor actual al editar.</param>
/// <param name="AutorizacionCarrera" example="false">El estudiante confirma que su carrera autoriza que este bloque coincida con otro. Opcional; si no se envía se conserva el valor actual.</param>
public record GuardarHorarioRequest(string Materia, int DiaSemana, string HoraInicio, string HoraFin, string? Aula, bool? Asiste = null, bool? AutorizacionCarrera = null);

/// <summary>Otro bloque del mismo día que se cruza con el consultado.</summary>
/// <param name="Id" example="13">Identificador del otro bloque.</param>
/// <param name="Materia" example="MAT-241">Materia del otro bloque.</param>
/// <param name="HoraInicio" example="14:15:00">Hora de inicio del otro bloque.</param>
/// <param name="HoraFin" example="15:45:00">Hora de fin del otro bloque.</param>
/// <param name="Asiste" example="true">Si el estudiante asiste al otro bloque.</param>
public record CoincidenciaDto(int Id, string Materia, string HoraInicio, string HoraFin, bool Asiste);

/// <summary>Cambio de asistencia de un bloque.</summary>
/// <param name="Asiste" example="false">Si el estudiante asiste al bloque.</param>
public record CambiarAsistenciaRequest(bool Asiste);

/// <summary>Cambio de asistencia de un bloque dentro de un lote.</summary>
/// <param name="Id" example="12">Identificador del bloque.</param>
/// <param name="Asiste" example="false">Si el estudiante asiste al bloque.</param>
public record AsistenciaBloqueRequest(int Id, bool Asiste);

/// <summary>Cambios de asistencia de varios bloques a la vez (hasta 40).</summary>
public record CambiarAsistenciasRequest(List<AsistenciaBloqueRequest> Bloques);

/// <summary>Bloque de clase del estudiante.</summary>
/// <param name="Id" example="12">Identificador del bloque.</param>
/// <param name="Materia" example="Cálculo I">Nombre de la materia.</param>
/// <param name="DiaSemana" example="1">Día de la semana: 1 = lunes … 6 = sábado.</param>
/// <param name="HoraInicio" example="08:00:00">Hora de inicio (<c>HH:mm:ss</c>, 24 h).</param>
/// <param name="HoraFin" example="09:30:00">Hora de fin (<c>HH:mm:ss</c>, 24 h).</param>
/// <param name="Aula" example="A-101">Aula, o <c>null</c> si no se indicó.</param>
/// <param name="Origen" example="MANUAL">Cómo se cargó el bloque: <c>MANUAL</c> o <c>FOTO</c>. El cliente no lo envía.</param>
/// <param name="Asiste" example="true">Si el estudiante asiste a este bloque. Si es <c>false</c> no cuenta como tiempo ocupado.</param>
/// <param name="AutorizacionCarrera" example="false">El estudiante confirmó que su carrera autoriza el choque.</param>
/// <param name="Coincide">Bloques del mismo día que se cruzan con este, asista o no el estudiante (sirve para preguntarle a cuál asiste).</param>
/// <param name="Advertencias" example="[&quot;Coincide con «MAT-241» (14:15-15:45).&quot;]">Avisos de choque: solo si el estudiante asiste a este bloque y al otro. No impiden guardar.</param>
public record HorarioDto(
    int Id, string Materia, int DiaSemana, string HoraInicio, string HoraFin, string? Aula, string Origen,
    bool Asiste, bool AutorizacionCarrera, List<CoincidenciaDto> Coincide, List<string> Advertencias);

/// <summary>Error de negocio con un código que la Api traduce a HTTP.</summary>
public class ReglaNegocioException : Exception
{
    public string Codigo { get; }   // VALIDACION | DUPLICADO | NO_ENCONTRADO
    public ReglaNegocioException(string codigo, string mensaje) : base(mensaje) => Codigo = codigo;
}
