namespace Breaku.Application.Actividades;

/// <summary>Texto libre del estudiante para extraer una actividad.</summary>
/// <param name="Texto" example="Quiero jugar cartas por una hora">Lo que quiere hacer, en sus palabras. Obligatorio, máximo 200 caracteres. Si se responde a una pregunta, es solo la respuesta (por ejemplo «a las 4»).</param>
/// <param name="Anterior">Lo que ya se había entendido (el campo <c>actividad</c> de la respuesta anterior). Se envía solo para responder la pregunta por un dato faltante; en la primera consulta va <c>null</c>.</param>
public record SolicitudExtraccionDto(string? Texto, ActividadExtraidaDto? Anterior);

/// <summary>Datos de la actividad. Un campo en <c>null</c> es un dato que falta (ver <c>faltantes</c>).</summary>
/// <param name="Actividad" example="jugar cartas">Qué se va a hacer, en pocas palabras (máximo 100 caracteres).</param>
/// <param name="HoraInicio" example="16:00">Hora de inicio de hoy, en hora de Bolivia (<c>HH:mm</c>, 24 h), entre 07:15 y 21:00.</param>
/// <param name="DuracionMin" example="60">Duración en minutos, de 10 a 240.</param>
/// <param name="MaxParticipantes" example="4">Personas en total contando a quien la crea, de 2 a 30.</param>
public record ActividadExtraidaDto(string? Actividad, string? HoraInicio, int? DuracionMin, int? MaxParticipantes);

/// <summary>Resultado de la extracción.</summary>
/// <param name="ConsultaId" example="58">Identificador del registro de auditoría de esta extracción (sirve como <c>log_id</c> de la comunidad que se cree con estos datos).</param>
/// <param name="Estado" example="INCOMPLETO"><c>COMPLETO</c> = están todos los datos. <c>INCOMPLETO</c> = se entendió la actividad, pero falta algún dato: <c>mensaje</c> trae la pregunta. <c>NO_ENTENDIDO</c> = no se reconoce ninguna actividad.</param>
/// <param name="Mensaje" example="¿Cuánto tiempo va a durar la actividad?">Pregunta por el primer dato faltante (<c>INCOMPLETO</c>) o «No entendimos tu actividad, ¿puedes describirla con más detalle?» (<c>NO_ENTENDIDO</c>). <c>null</c> si está completo.</param>
/// <param name="Actividad">Datos entendidos y válidos hasta ahora.</param>
/// <param name="Faltantes">Campos que faltan o no son válidos (<c>actividad</c>, <c>horaInicio</c>, <c>duracionMin</c>, <c>maxParticipantes</c>), en el orden en que se preguntan.</param>
/// <param name="Supuestos">Campos que completó el sistema porque el estudiante no los dijo: hoy solo <c>horaInicio</c> (sin hora, la actividad empieza ahora). El frontend debe mostrarlo para que se pueda corregir.</param>
public record RespuestaExtraccionDto(
    long ConsultaId,
    string Estado,
    string? Mensaje,
    ActividadExtraidaDto Actividad,
    List<string> Faltantes,
    List<string> Supuestos
);
