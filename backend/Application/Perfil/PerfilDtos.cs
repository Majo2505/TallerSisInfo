namespace Breaku.Application.Perfil;

/// <summary>Perfil del usuario autenticado (HU21).</summary>
/// <param name="Nombre" example="Ana Pérez">Nombre completo. Editable.</param>
/// <param name="Correo" example="ana.perez@ucb.edu.bo">Correo institucional. No editable.</param>
/// <param name="Rol" example="ESTUDIANTE">Rol. No editable.</param>
/// <param name="Carrera" example="Ingeniería de Sistemas">Carrera, o <c>null</c> si no la completó. Editable.</param>
/// <param name="Semestre" example="5">Semestre de 1 a 10, o <c>null</c>. Editable.</param>
public record PerfilDto(string Nombre, string Correo, string Rol, string? Carrera, int? Semestre);

/// <summary>Cambios del perfil. Solo se editan nombre, carrera y semestre.</summary>
/// <param name="Nombre" example="Ana Pérez">Obligatorio, hasta 100 caracteres.</param>
/// <param name="Carrera" example="Ingeniería de Sistemas">Opcional, hasta 80 caracteres. Vacía se guarda como <c>null</c>.</param>
/// <param name="Semestre" example="5">Opcional, de 1 a 10.</param>
/// <param name="Correo">No editable: si se envía distinto al actual, se rechaza con <c>NO_EDITABLE</c>.</param>
/// <param name="Rol">No editable: si se envía distinto al actual, se rechaza con <c>NO_EDITABLE</c>.</param>
public record ActualizarPerfilRequest(string? Nombre, string? Carrera, int? Semestre, string? Correo = null, string? Rol = null);
