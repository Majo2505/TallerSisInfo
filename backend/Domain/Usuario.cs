namespace Breaku.Domain;

public class Usuario
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Carrera { get; set; }                       // HU21: opcional, editable desde el perfil
    public byte? Semestre { get; set; }                        // HU21: 1 a 10, opcional
    public string Rol { get; set; } = null!;
    public bool CorreoVerificado { get; set; }
    public bool ConsentimientoUbicacion { get; set; }
    public DateTime? FechaConsentimiento { get; set; }
    public bool NotificacionesActivas { get; set; } = true;   // HU4: false = no se le envían avisos
    public bool VisibleMatching { get; set; }                 // HU12: true = compañeros de su sede con tiempo compatible lo encuentran
    public DateTime? FechaVisibleMatching { get; set; }       // HU12: cuándo lo aceptó
    public int? ZonaActualId { get; set; }
    public Zona? ZonaActual { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public bool Activo { get; set; }
}
