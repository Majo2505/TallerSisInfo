namespace Breaku.Application.Comunidades;

/// <summary>Compañero al que se invitó. Sin correo ni id: solo lo necesario para saber quién es.</summary>
/// <param name="Nombre" example="Diego Mamani">Nombre del compañero.</param>
/// <param name="Carrera" example="Ingeniería Industrial">Su carrera, si la registró.</param>
/// <param name="LibreDesde" example="16:00">Desde cuándo comparte la actividad (<c>HH:mm</c>, hora de Bolivia).</param>
/// <param name="LibreHasta" example="17:00">Hasta cuándo (<c>HH:mm</c>).</param>
/// <param name="MinutosEnComun" example="60">Minutos de la actividad en los que está libre.</param>
public record InvitadoDto(string Nombre, string? Carrera, string LibreDesde, string LibreHasta, int MinutosEnComun);

/// <summary>HU13: a quiénes se invitó automáticamente a la actividad (o por qué no se invitó a nadie).</summary>
/// <param name="Estado" example="INVITADOS">
/// <c>INVITADOS</c>: se invitó y se avisó a compañeros libres a esa hora. <c>SIN_COINCIDENCIAS</c>: nadie de la sede está libre.
/// <c>SIN_ZONA</c>: el estudiante no eligió campus. <c>NO_VISIBLE</c>: no aceptó aparecer en el matching (HU12) y, como el
/// consentimiento es recíproco, tampoco invita. <c>COMPLETA</c>: el grupo ya no tiene lugar. <c>ERROR</c>: la actividad quedó
/// creada, pero no se pudo invitar (se puede volver a intentar).
/// </param>
/// <param name="Mensaje" example="Invitamos a 3 compañeros libres a esa hora.">Texto listo para mostrar.</param>
/// <param name="Invitados">A quiénes se invitó en este intento, del más al menos compatible.</param>
public record InvitacionDto(string Estado, string Mensaje, List<InvitadoDto> Invitados);
