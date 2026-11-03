using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Comunidades;

/// <summary>Compañero que podría recibir la invitación, con sus puentes de ese día.</summary>
/// <param name="UsuarioId">El compañero.</param>
/// <param name="Nombre">Su nombre.</param>
/// <param name="Carrera">Su carrera, si la registró.</param>
/// <param name="NotificacionesActivas">HU4: si es false, se lo invita igual pero no se le envía el aviso.</param>
/// <param name="Puentes">Sus puentes del día de la actividad.</param>
public record CandidatoComunidad(int UsuarioId, string Nombre, string? Carrera, bool NotificacionesActivas, List<Puente> Puentes);

/// <summary>HU13: invita automáticamente a una actividad recién creada (lo usa HU10 al crearla).</summary>
public interface IInvitadorComunidad
{
    Task<InvitacionDto> InvitarAsync(ComunidadEfimera comunidad, CancellationToken ct = default);
}

/// <summary>Puerto: lo implementa Infrastructure.</summary>
public interface IComunidadRepository
{
    /// <summary>Usuario activo con su sede, o <c>null</c>.</summary>
    Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default);

    /// <summary>La comunidad, o <c>null</c> si no existe.</summary>
    Task<ComunidadEfimera?> ObtenerComunidadAsync(int comunidadId, CancellationToken ct = default);

    /// <summary>Ids de quienes ya están en la comunidad (cualquier estado).</summary>
    Task<List<int>> ListarParticipantesAsync(int comunidadId, CancellationToken ct = default);

    /// <summary>
    /// Estudiantes activos de la sede que aceptaron aparecer en el matching, con algún puente ese día que se solape con
    /// la actividad. Excluye al creador, a quien ya está en esta comunidad y a quien ya está (creó o confirmó) en otra
    /// comunidad activa a esa hora.
    /// </summary>
    /// <param name="actividad">Día y horas de la actividad, en hora de Bolivia (como los puentes).</param>
    Task<List<CandidatoComunidad>> ListarCandidatosAsync(ComunidadEfimera comunidad, int zonaId, ReglasMatching.Franja actividad,
        CancellationToken ct = default);

    /// <summary>Guarda las participaciones y los avisos en una sola transacción.</summary>
    Task GuardarInvitacionesAsync(IReadOnlyList<Participacion> participaciones, IReadOnlyList<Notificacion> avisos,
        CancellationToken ct = default);
}
