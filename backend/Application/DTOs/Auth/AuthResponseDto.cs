namespace Breaku.Application.DTOs.Auth;

/// <summary>Sesión iniciada: token JWT y datos básicos del usuario.</summary>
public class AuthResponseDto
{
    /// <summary>Token JWT (HS256, vigencia de 24 horas). Se envía en el header <c>Authorization: Bearer {token}</c>.</summary>
    /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI3IiwiZW1haWwiOiJkZXYuZXN0dWRpYW50ZUB1Y2IuZWR1LmJvIn0.firma</example>
    public string Token { get; set; } = string.Empty;

    /// <summary>Identificador del usuario.</summary>
    /// <example>7</example>
    public int UserId { get; set; }

    /// <summary>Nombre completo del usuario.</summary>
    /// <example>Ana Pérez</example>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Correo institucional del usuario.</summary>
    /// <example>ana.perez@ucb.edu.bo</example>
    public string Email { get; set; } = string.Empty;

    /// <summary>Rol del usuario: <c>ESTUDIANTE</c> o <c>ADMIN</c>. El frontend lo usa para mostrar el panel de administración.</summary>
    /// <example>ESTUDIANTE</example>
    public string Rol { get; set; } = string.Empty;
}
