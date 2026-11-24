namespace Breaku.Domain;

public class Notificacion
{
    public const string TipoPuenteDisponible = "PUENTE_DISPONIBLE";
    public const string TipoDespuesDeClases = "DESPUES_DE_CLASES";
    public const string TipoInvitacionComunidad = "INVITACION_COMUNIDAD";
    public const string TipoComunidadCerrada = "COMUNIDAD_CERRADA";
    public const string TipoEventoAprobado = "EVENTO_APROBADO";
    public const string TipoEventoRechazado = "EVENTO_RECHAZADO";
    public const string TipoEventoBajado = "EVENTO_BAJADO";
    /// <summary>HU28: aviso al estudiante por una advertencia. La suspensión y la baja no notifican.</summary>
    public const string TipoSancionAdvertencia = "SANCION_ADVERTENCIA";
    /// <summary>HU27: un administrador resolvió o descartó un reporte tuyo (con su respuesta, recortada).</summary>
    public const string TipoReporteResuelto = "REPORTE_RESUELTO";
    /// <summary>HU12: alguien de «Libres contigo» te saludó.</summary>
    public const string TipoConexionSolicitada = "CONEXION_SOLICITADA";
    /// <summary>HU12: la persona que saludaste aceptó; ya pueden ver su correo.</summary>
    public const string TipoConexionAceptada = "CONEXION_ACEPTADA";
    /// <summary>Para el admin: un estudiante envió un reporte (migración 030).</summary>
    public const string TipoReporteNuevo = "REPORTE_NUEVO";
    /// <summary>Para el admin: un evento quedó pendiente de revisión (migración 030).</summary>
    public const string TipoEventoPorRevisar = "EVENTO_POR_REVISAR";
    /// <summary>
    /// Para quien creó una comunidad: alguien se sumó o se bajó (migración 031). Uno por comunidad: el siguiente cambio
    /// reemplaza el mensaje y lo vuelve a dejar sin leer.
    /// </summary>
    public const string TipoAsistenciaComunidad = "ASISTENCIA_COMUNIDAD";
    /// <summary>Una conexión de «Libres contigo» te invita a un evento de hoy (migración 032). Lleva <c>evento_id</c>.</summary>
    public const string TipoInvitacionEvento = "INVITACION_EVENTO";
    /// <summary>HU32, para el admin: un estudiante llegó a 3 intentos bloqueados por seguridad en 24 h. Lleva <c>usuario_objetivo_id</c>.</summary>
    public const string TipoAlertaUsuarioSospechoso = "ALERTA_USUARIO_SOSPECHOSO";
    /// <summary>HU32, para el admin: 3 o más reportes pendientes de personas distintas sobre lo que creó un estudiante.</summary>
    public const string TipoAlertaReportesAcumulados = "ALERTA_REPORTES_ACUMULADOS";

    public long Id { get; set; }
    public int UsuarioId { get; set; }
    public string Tipo { get; set; } = "";
    public int? ComunidadId { get; set; }
    public int? EventoId { get; set; }
    /// <summary>Solo en REPORTE_RESUELTO: el reporte al que apunta el aviso.</summary>
    public int? ReporteId { get; set; }
    /// <summary>Solo en SANCION_ADVERTENCIA: la sanción a la que apunta el aviso.</summary>
    public int? SancionId { get; set; }
    /// <summary>Solo en CONEXION_SOLICITADA y CONEXION_ACEPTADA: el saludo al que apunta el aviso.</summary>
    public int? ConexionId { get; set; }
    /// <summary>HU32: solo en las alertas al admin, el estudiante del que se avisa (migración 033).</summary>
    public int? UsuarioObjetivoId { get; set; }

    public DateOnly? PuenteFecha { get; set; }
    public TimeOnly? PuenteHoraInicio { get; set; }
    public TimeOnly? PuenteHoraFin { get; set; }

    public string Mensaje { get; set; } = "";
    public bool Leida { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
