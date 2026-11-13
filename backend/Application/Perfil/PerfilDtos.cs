namespace Breaku.Application.Perfil;

/// <param name="Nombre">Nombre completo (editable).</param>
/// <param name="Correo">Correo institucional (no editable).</param>
/// <param name="Rol">ESTUDIANTE o ADMIN (no editable).</param>
/// <param name="Carrera">Carrera (opcional).</param>
/// <param name="Semestre">Semestre de 1 a 10 (opcional).</param>
/// <param name="TieneContrasena">False si entra solo con Google: en Seguridad se le pide crear una, sin la actual.</param>
/// <param name="ConGoogle">Si la cuenta está enlazada con Google.</param>
public record PerfilDto(string Nombre, string Correo, string Rol, string? Carrera, int? Semestre, bool TieneContrasena = true, bool ConGoogle = false);

/// <param name="Actual">La contraseña actual (no hace falta si la cuenta todavía no tiene, porque entra con Google).</param>
/// <param name="Nueva">Al menos 8 caracteres, con una letra y un número.</param>
/// <param name="Confirmacion">La nueva otra vez.</param>
public record CambiarContrasenaRequest(string? Actual, string? Nueva, string? Confirmacion);

/// <param name="Confirmacion">El texto ELIMINAR.</param>
/// <param name="Contrasena">Su contraseña (cuentas con contraseña).</param>
/// <param name="CredencialGoogle">El token de un nuevo login con Google (cuentas enlazadas con Google).</param>
public record EliminarCuentaRequest(string? Confirmacion, string? Contrasena, string? CredencialGoogle);

public record ActualizarPerfilRequest(string? Nombre, string? Carrera, int? Semestre, string? Correo = null, string? Rol = null);
