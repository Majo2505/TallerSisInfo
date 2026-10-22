namespace Breaku.Application.Notificaciones;

/// <summary>Aviso para el estudiante. Hoy solo existe el tipo <c>PUENTE_DISPONIBLE</c> (HU4).</summary>
/// <param name="Id" example="31">Identificador de la notificación.</param>
/// <param name="Tipo" example="PUENTE_DISPONIBLE">Tipo de aviso: <c>PUENTE_DISPONIBLE</c>, <c>INVITACION_COMUNIDAD</c> o <c>COMUNIDAD_CERRADA</c>.</param>
/// <param name="Mensaje" example="¡Puente disponible! Tienes 1 h 30 min libres, de 10:00 a 11:30.">Texto listo para mostrar al usuario.</param>
/// <param name="Fecha" example="2026-10-22">Día del puente (<c>yyyy-MM-dd</c>, hora de Bolivia). <c>null</c> si no es un aviso de puente.</param>
/// <param name="HoraInicio" example="10:00:00">Inicio del puente (<c>HH:mm:ss</c>). <c>null</c> si no es un aviso de puente.</param>
/// <param name="HoraFin" example="11:30:00">Fin del puente (<c>HH:mm:ss</c>). <c>null</c> si no es un aviso de puente.</param>
/// <param name="DuracionMinutos" example="90">Duración disponible del puente en minutos. <c>null</c> si no es un aviso de puente.</param>
/// <param name="Leida" example="false">Si el usuario ya la marcó como leída.</param>
/// <param name="CreadaEn" example="2026-10-22T14:00:04.512Z">Momento del envío (UTC).</param>
public record NotificacionDto(
    long Id,
    string Tipo,
    string Mensaje,
    string? Fecha,
    string? HoraInicio,
    string? HoraFin,
    int? DuracionMinutos,
    bool Leida,
    DateTime CreadaEn
);

/// <summary>Preferencia de avisos del usuario.</summary>
/// <param name="NotificacionesActivas" example="true">
/// <c>true</c> = recibe avisos de puente. <c>false</c> = no recibe ninguno, aunque sus puentes se siguen detectando.
/// </param>
public record ConfiguracionNotificacionesDto(bool NotificacionesActivas);
