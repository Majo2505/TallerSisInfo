namespace Breaku.Application.DTOs;

public record ErrorMensajeDto(string Message);

public record ErrorNegocioDto(string Codigo, string Mensaje);

/// <summary>HU28: la cuenta está suspendida o dada de baja. <c>Message</c> es el texto listo para mostrar.</summary>
public record ErrorCuentaBloqueadaDto(string Message, string Codigo, string Motivo, DateTime? Hasta);
