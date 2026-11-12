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
    Task<IReadOnlyList<int>> ArchivarVencidasAsync(DateTime ahoraUtc, CancellationToken ct = default);

    /// <summary>
    /// HU15: apaga los avisos (invitación, «el grupo se llenó») de las comunidades archivadas, para que no queden en la
    /// campanita de algo que ya terminó. Con <paramref name="comunidadIds"/> en null, los de todas las archivadas.
    /// </summary>
    Task<int> DesactivarAvisosDeArchivadasAsync(IReadOnlyCollection<int>? comunidadIds, CancellationToken ct = default);

    Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default);

    Task<ComunidadEfimera?> ObtenerComunidadAsync(int comunidadId, CancellationToken ct = default);

    Task<List<int>> ListarParticipantesAsync(int comunidadId, CancellationToken ct = default);

    Task<List<CandidatoComunidad>> ListarCandidatosAsync(ComunidadEfimera comunidad, int zonaId, ReglasMatching.Franja actividad,
        CancellationToken ct = default);

    Task GuardarInvitacionesAsync(IReadOnlyList<Participacion> participaciones, IReadOnlyList<Notificacion> avisos,
        CancellationToken ct = default);
}
