namespace Breaku.Application.Comunidades;

public record InvitadoDto(string Nombre, string? Carrera, string LibreDesde, string LibreHasta, int MinutosEnComun);

public record InvitacionDto(string Estado, string Mensaje, List<InvitadoDto> Invitados);
