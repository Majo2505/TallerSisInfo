namespace Breaku.Application.DTOs;

/// <summary>Error de autenticación (login y registro).</summary>
/// <param name="Message" example="Solo se permite el acceso con un correo institucional (@ucb.edu.bo)">Mensaje en español, listo para mostrar al usuario.</param>
public record ErrorMensajeDto(string Message);

/// <summary>Error de negocio de horarios.</summary>
/// <param name="Codigo" example="SOLAPAMIENTO">Código del error: <c>VALIDACION</c> (400), <c>SOLAPAMIENTO</c> (409) o <c>NO_ENCONTRADO</c> (404).</param>
/// <param name="Mensaje" example="Se solapa con «Cálculo I» (08:00-09:30).">Mensaje en español, listo para mostrar al usuario.</param>
public record ErrorNegocioDto(string Codigo, string Mensaje);
