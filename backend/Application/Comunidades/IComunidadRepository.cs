using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Comunidades;

/// <summary>Compañero que podría recibir la invitación, con sus puentes de ese día.</summary>
/// <param name="UsuarioId">El compañero.</param>
/// <param name="Nombre">Su nombre.</param>
/// <param name="Carrera">Su carrera, si la registró.</param>
/// <param name="Puentes">Sus puentes del día de la actividad.</param>
/// <param name="NotificacionesActivas">HU4: si es false, se lo invita igual pero no se le envía el aviso.</param>
public record CandidatoComunidad(int UsuarioId, string Nombre, string? Carrera, bool NotificacionesActivas, List<Puente> Puentes);

/// <summary>Puerto: lo implementa Infrastructure.</summary>
public interface IComunidadRepository
{
    /// <summary>Usuario activo con su sede, o <c>null</c>.</summary>
    Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default);

    /// <summary>El registro de auditoría de una extracción (HU11), o <c>null</c> si no existe.</summary>
    Task<LogAuditoriaRag?> ObtenerExtraccionAsync(long consultaId, CancellationToken ct = default);

    /// <summary>Si esa extracción ya creó una comunidad.</summary>
    Task<bool> ExisteComunidadDeConsultaAsync(long consultaId, CancellationToken ct = default);

    /// <summary>
    /// Si el usuario ya está (creó o confirmó) en otra comunidad activa que se cruza con ese horario (UTC).
    /// </summary>
    Task<bool> EstaEnOtraComunidadAsync(int usuarioId, DateTime inicioUtc, DateTime finUtc, CancellationToken ct = default);

    /// <summary>
    /// Estudiantes activos de la sede que aceptaron aparecer en el matching, con algún puente ese día que se solape con
    /// la actividad, y que no estén ya en otra comunidad a esa hora. Excluye al propio usuario.
    /// </summary>
    /// <param name="actividad">Día y horas de la actividad, en hora de Bolivia (como los puentes).</param>
    Task<List<CandidatoComunidad>> ListarCandidatosAsync(int usuarioId, int zonaId, ReglasMatching.Franja actividad,
        DateTime inicioUtc, DateTime finUtc, CancellationToken ct = default);

    /// <summary>
    /// Guarda la comunidad, las participaciones y los avisos en una sola transacción. Al volver, <c>comunidad.Id</c> ya
    /// está asignado. Si la extracción ya tenía comunidad (uq_com_log), lanza <c>YA_CREADA</c>.
    /// </summary>
    Task CrearAsync(ComunidadEfimera comunidad, IReadOnlyList<Participacion> participaciones, IReadOnlyList<Notificacion> avisos,
        CancellationToken ct = default);
}
