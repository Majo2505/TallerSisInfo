namespace Breaku.Application.DTOs;

public record ErrorMensajeDto(string Message);

public record ErrorNegocioDto(string Codigo, string Mensaje);

/// <summary>HU37: un error que nadie esperaba (500). <c>CodigoSeguimiento</c> es el mismo que queda en el log del servidor.</summary>
public record ErrorInesperadoDto(string Codigo, string Mensaje, string CodigoSeguimiento);

/// <summary>HU28: la cuenta está suspendida o dada de baja. <c>Message</c> es el texto listo para mostrar.</summary>
public record ErrorCuentaBloqueadaDto(string Message, string Codigo, string Motivo, DateTime? Hasta);
