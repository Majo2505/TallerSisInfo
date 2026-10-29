namespace Breaku.Application.DTOs;

/// <summary>Error de autenticación (login y registro).</summary>
/// <param name="Message" example="Solo se permite el acceso con un correo institucional (@ucb.edu.bo)">Mensaje en español, listo para mostrar al usuario.</param>
public record ErrorMensajeDto(string Message);

/// <summary>Error de negocio de horarios.</summary>
/// <param name="Codigo" example="DUPLICADO">Código del error: <c>VALIDACION</c> (400), <c>DUPLICADO</c> (409) o <c>NO_ENCONTRADO</c> (404).</param>
/// <param name="Mensaje" example="Ya registraste «Cálculo I» a esa hora ese día.">Mensaje en español, listo para mostrar al usuario.</param>
public record ErrorNegocioDto(string Codigo, string Mensaje);
