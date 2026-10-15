namespace Breaku.Application.DTOs.Auth;

/// <summary>Datos para crear una cuenta de estudiante.</summary>
public class RegisterRequestDto
{
    /// <summary>Nombre completo del estudiante.</summary>
    /// <example>Ana Pérez</example>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Correo institucional. Solo se aceptan direcciones <c>usuario@ucb.edu.bo</c> y no puede estar ya registrado.</summary>
    /// <example>ana.perez@ucb.edu.bo</example>
    public string Email { get; set; } = string.Empty;

    /// <summary>Contraseña de la cuenta. Se guarda con hash BCrypt.</summary>
    /// <example>MiClaveSegura-2026</example>
    public string Password { get; set; } = string.Empty;
}
