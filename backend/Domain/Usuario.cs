namespace Breaku.Domain;

public class Usuario
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    /// <summary>Cuenta de Google enlazada (claim «sub»); null si nunca entró con Google.</summary>
    public string? GoogleSub { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Carrera { get; set; }
    public byte? Semestre { get; set; }
    public string Rol { get; set; } = null!;
    public bool CorreoVerificado { get; set; }
    public bool ConsentimientoUbicacion { get; set; }
    public DateTime? FechaConsentimiento { get; set; }
    public bool NotificacionesActivas { get; set; } = true;
    public bool VisibleMatching { get; set; }
    public DateTime? FechaVisibleMatching { get; set; }
    public int? ZonaActualId { get; set; }
    public Zona? ZonaActual { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public bool Activo { get; set; }
}
