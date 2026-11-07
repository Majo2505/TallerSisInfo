namespace Breaku.Domain;

public class Notificacion
{
    public const string TipoPuenteDisponible = "PUENTE_DISPONIBLE";
    public const string TipoDespuesDeClases = "DESPUES_DE_CLASES";
    public const string TipoInvitacionComunidad = "INVITACION_COMUNIDAD";
    public const string TipoComunidadCerrada = "COMUNIDAD_CERRADA";

    public long Id { get; set; }
    public int UsuarioId { get; set; }
    public string Tipo { get; set; } = "";
    public int? ComunidadId { get; set; }

    public DateOnly? PuenteFecha { get; set; }
    public TimeOnly? PuenteHoraInicio { get; set; }
    public TimeOnly? PuenteHoraFin { get; set; }

    public string Mensaje { get; set; } = "";
    public bool Leida { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
