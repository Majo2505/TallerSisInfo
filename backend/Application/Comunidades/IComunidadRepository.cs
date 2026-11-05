using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Comunidades;

public record CandidatoComunidad(int UsuarioId, string Nombre, string? Carrera, bool NotificacionesActivas, List<Puente> Puentes);

public interface IInvitadorComunidad
{
    Task<InvitacionDto> InvitarAsync(ComunidadEfimera comunidad, CancellationToken ct = default);
}

public interface IComunidadRepository
{
    Task<int> ArchivarVencidasAsync(DateTime ahoraUtc, CancellationToken ct = default);

    Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default);

    Task<ComunidadEfimera?> ObtenerComunidadAsync(int comunidadId, CancellationToken ct = default);

    Task<List<int>> ListarParticipantesAsync(int comunidadId, CancellationToken ct = default);

    Task<List<CandidatoComunidad>> ListarCandidatosAsync(ComunidadEfimera comunidad, int zonaId, ReglasMatching.Franja actividad,
        CancellationToken ct = default);

    Task GuardarInvitacionesAsync(IReadOnlyList<Participacion> participaciones, IReadOnlyList<Notificacion> avisos,
        CancellationToken ct = default);
}
