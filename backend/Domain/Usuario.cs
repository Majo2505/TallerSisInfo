namespace Breaku.Domain;

public class Usuario
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string Nombre { get; set; } = null!;
    public string Rol { get; set; } = null!;
    public bool CorreoVerificado { get; set; }
    public bool ConsentimientoUbicacion { get; set; }
    public DateTime? FechaConsentimiento { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public bool Activo { get; set; }
}
