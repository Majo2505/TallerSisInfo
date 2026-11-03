namespace Breaku.Application.Perfil;

public record PerfilDto(string Nombre, string Correo, string Rol, string? Carrera, int? Semestre);

public record ActualizarPerfilRequest(string? Nombre, string? Carrera, int? Semestre, string? Correo = null, string? Rol = null);
