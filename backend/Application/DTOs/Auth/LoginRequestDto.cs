namespace Breaku.Application.DTOs.Auth;

/// <summary>Credenciales para iniciar sesión.</summary>
public class LoginRequestDto
{
    /// <summary>Correo institucional. Solo se aceptan direcciones <c>usuario@ucb.edu.bo</c>.</summary>
    /// <example>dev.estudiante@ucb.edu.bo</example>
    public string Email { get; set; } = string.Empty;

    /// <summary>Contraseña de la cuenta.</summary>
    /// <example>Breaku-Dev-2026!</example>
    public string Password { get; set; } = string.Empty;
}
