using Breaku.Application.Actividades;

namespace Breaku.Application.Comunidades;

/// <summary>Pedido para crear la comunidad con una actividad ya extraída (HU11).</summary>
/// <param name="ConsultaId" example="58">El <c>consultaId</c> que devolvió <c>POST /api/actividades/extraer</c> con estado <c>COMPLETO</c>.</param>
/// <param name="Texto" example="Quiero jugar cartas a las 4 por una hora">Lo que escribió el estudiante (opcional, máximo 200 caracteres). Si falta, se guarda la actividad.</param>
/// <param name="Actividad">Los datos de la extracción, tal cual llegaron (<c>actividad</c>, <c>horaInicio</c>, <c>duracionMin</c>, <c>maxParticipantes</c>).</param>
public record CrearComunidadRequest(long? ConsultaId, string? Texto, ActividadExtraidaDto? Actividad);

/// <summary>Compañero al que se invitó. Sin correo ni id: solo lo necesario para saber quién es.</summary>
/// <param name="Nombre" example="Diego Mamani">Nombre del compañero.</param>
/// <param name="Carrera" example="Ingeniería Industrial">Su carrera, si la registró.</param>
/// <param name="LibreDesde" example="10:30">Desde cuándo comparte la actividad (<c>HH:mm</c>, hora de Bolivia).</param>
/// <param name="LibreHasta" example="11:30">Hasta cuándo (<c>HH:mm</c>).</param>
/// <param name="MinutosEnComun" example="60">Minutos de la actividad en los que está libre.</param>
public record InvitadoDto(string Nombre, string? Carrera, string LibreDesde, string LibreHasta, int MinutosEnComun);

/// <summary>La comunidad recién creada.</summary>
/// <param name="Id" example="12">Identificador de la comunidad.</param>
/// <param name="Actividad" example="jugar cartas">Qué se va a hacer.</param>
/// <param name="HoraInicio" example="10:30">Inicio de hoy (<c>HH:mm</c>, hora de Bolivia).</param>
/// <param name="HoraFin" example="11:30">Fin (<c>HH:mm</c>).</param>
/// <param name="MaxParticipantes" example="4">Personas en total, contando a quien la creó.</param>
/// <param name="Invitados">A quiénes se invitó, del más al menos compatible.</param>
public record ComunidadCreadaDto(int Id, string Actividad, string HoraInicio, string HoraFin, int MaxParticipantes, List<InvitadoDto> Invitados);

/// <summary>Resultado de pedir la comunidad.</summary>
/// <param name="Estado" example="CREADA">
/// <c>CREADA</c>: se creó y se avisó a los invitados. <c>SIN_COINCIDENCIAS</c>: nadie está libre a esa hora y no se creó nada.
/// <c>SIN_ZONA</c>: el estudiante no eligió campus. <c>NO_VISIBLE</c>: no aceptó aparecer en el matching (HU12) y,
/// como el consentimiento es recíproco, tampoco puede invitar.
/// </param>
/// <param name="Mensaje" example="¡Grupo armado! Invitamos a 3 compañeros libres a esa hora.">Texto listo para mostrar.</param>
/// <param name="Comunidad">La comunidad creada. <c>null</c> si no se creó.</param>
public record ResultadoComunidadDto(string Estado, string Mensaje, ComunidadCreadaDto? Comunidad);
