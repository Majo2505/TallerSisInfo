namespace Breaku.Domain;

public class Notificacion
{
    public const string TipoPuenteDisponible = "PUENTE_DISPONIBLE";
    public const string TipoDespuesDeClases = "DESPUES_DE_CLASES";
    public const string TipoInvitacionComunidad = "INVITACION_COMUNIDAD";
    public const string TipoComunidadCerrada = "COMUNIDAD_CERRADA";
    /// <summary>HU28: aviso al estudiante por una advertencia. La suspensión y la baja no notifican.</summary>
    public const string TipoSancionAdvertencia = "SANCION_ADVERTENCIA";
    /// <summary>HU12: alguien de «Libres contigo» te saludó.</summary>
    public const string TipoConexionSolicitada = "CONEXION_SOLICITADA";
    /// <summary>HU12: la persona que saludaste aceptó; ya pueden ver su correo.</summary>
    public const string TipoConexionAceptada = "CONEXION_ACEPTADA";

    public long Id { get; set; }
    public int UsuarioId { get; set; }
    public string Tipo { get; set; } = "";
    public int? ComunidadId { get; set; }
    /// <summary>Solo en SANCION_ADVERTENCIA: la sanción a la que apunta el aviso.</summary>
    public int? SancionId { get; set; }
    /// <summary>Solo en CONEXION_SOLICITADA y CONEXION_ACEPTADA: el saludo al que apunta el aviso.</summary>
    public int? ConexionId { get; set; }

    public DateOnly? PuenteFecha { get; set; }
    public TimeOnly? PuenteHoraInicio { get; set; }
    public TimeOnly? PuenteHoraFin { get; set; }

    public string Mensaje { get; set; } = "";
    public bool Leida { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
