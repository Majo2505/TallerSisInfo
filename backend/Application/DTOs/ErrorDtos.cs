namespace Breaku.Application.DTOs;

public record ErrorMensajeDto(string Message);

public record ErrorNegocioDto(string Codigo, string Mensaje);
