namespace Breaku.Application.Comunidades;

public record CrearComunidadRequest(
    string Texto,
    string Actividad,
    DateTime Inicio,
    DateTime Fin,
    short MaxParticipantes = 6,
    int? LugarId = null);

public record ComunidadDto(
    int Id,
    string Actividad,
    string Texto,
    DateTime Inicio,
    DateTime Fin,
    short MaxParticipantes,
    string Estado,
    int Participantes,
    string? Lugar);

public record ParticipacionDto(int ComunidadId, string Estado);
