namespace Breaku.Application.Notificaciones;

public record NotificacionDto(
    long Id,
    string Tipo,
    string Mensaje,
    string? Fecha,
    string? HoraInicio,
    string? HoraFin,
    int? DuracionMinutos,
    bool Leida,
    DateTime CreadaEn,
    int? ComunidadId = null
);

public record ConfiguracionNotificacionesDto(bool NotificacionesActivas);
