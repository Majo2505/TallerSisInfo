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
    int? ComunidadId = null,
    // HU27: en REPORTE_RESUELTO, el reporte (la campanita lleva a «Mis reportes»).
    int? ReporteId = null,
    // Avisos de eventos (por ejemplo INVITACION_EVENTO): la campanita lleva al detalle del evento.
    int? EventoId = null,
    // HU32: en las alertas al admin, el estudiante del que se avisa (la campanita lleva a Admin → Usuarios).
    int? UsuarioObjetivoId = null
);

public record ConfiguracionNotificacionesDto(bool NotificacionesActivas);
