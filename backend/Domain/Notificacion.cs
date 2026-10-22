namespace Breaku.Domain;

/// <summary>Aviso para el estudiante (tabla notificacion). HU4 usa el tipo PUENTE_DISPONIBLE.</summary>
public class Notificacion
{
    public const string TipoPuenteDisponible = "PUENTE_DISPONIBLE";

    public long Id { get; set; }
    public int UsuarioId { get; set; }
    public string Tipo { get; set; } = "";       // PUENTE_DISPONIBLE | INVITACION_COMUNIDAD | COMUNIDAD_CERRADA
    public int? ComunidadId { get; set; }

    // Copia del puente avisado (solo PUENTE_DISPONIBLE), en hora local de Bolivia.
    // No hay puente_id: los puentes se recalculan en cada cambio de horario y su id cambia.
    public DateOnly? PuenteFecha { get; set; }
    public TimeOnly? PuenteHoraInicio { get; set; }
    public TimeOnly? PuenteHoraFin { get; set; }

    public string Mensaje { get; set; } = "";
    public bool Leida { get; set; }
    public bool Activa { get; set; } = true;     // false si el puente dejó de existir
    public DateTime CreatedAt { get; set; }      // UTC: momento del envío
}
