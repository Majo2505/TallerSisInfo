using Breaku.Domain;

namespace Breaku.Application.Actividades;

public interface IActividadRepository
{
    Task<LogAuditoriaRag?> ObtenerExtraccionAsync(long consultaId, int usuarioId, CancellationToken ct = default);

    Task<bool> ConsultaYaUsadaAsync(long consultaId, CancellationToken ct = default);

    Task AgregarAsync(ComunidadEfimera comunidad, CancellationToken ct = default);
}

public record CrearActividadRequest(string Texto, string Actividad, string HoraInicio, int DuracionMin, int MaxParticipantes, long ConsultaId);

public record ActividadCreadaDto(int Id, string Actividad, string HoraInicio, string HoraFin, int DuracionMin, int MaxParticipantes, string Estado,
    Breaku.Application.Comunidades.InvitacionDto? Invitacion = null);
