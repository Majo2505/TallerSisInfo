using Breaku.Domain.Rules;

namespace Breaku.Application.Sanciones;

/// <summary>
/// Un método por paso de la purga (<see cref="ReglasPurga.Paso"/>). Cada uno borra de forma explícita, sin depender de
/// ON DELETE CASCADE. El orden y la transacción los pone <see cref="PurgaUsuarioService"/>.
/// </summary>
public interface IPurgaRepository
{
    /// <summary>Las comunidades que creó el usuario, con sus participantes.</summary>
    Task<List<ReglasPurga.ComunidadCreada>> ListarComunidadesCreadasAsync(int usuarioId, CancellationToken ct = default);

    /// <summary>La comunidad sigue abierta con otro creador: <c>log_id</c> en NULL y <c>texto_original</c> vacío.</summary>
    Task CambiarCreadorAsync(int comunidadId, int nuevoCreadorId, DateTime ahoraUtc, CancellationToken ct = default);

    /// <summary>Borra la comunidad con sus participaciones, sus avisos y los reportes que apuntan a ella.</summary>
    Task EliminarComunidadAsync(int comunidadId, CancellationToken ct = default);

    /// <summary>Quita al usuario de las comunidades de otros. Devuelve las ACTIVAS donde estaba confirmado, con su cupo nuevo.</summary>
    Task<List<(int ComunidadId, int Confirmados)>> EliminarParticipacionesAsync(int usuarioId, CancellationToken ct = default);

    Task EliminarNotificacionesAsync(int usuarioId, CancellationToken ct = default);

    /// <summary>HU12: borra sus conexiones (y antes los avisos de otros que apuntan a ellas) y su test de afinidad.</summary>
    Task EliminarConexionesAsync(int usuarioId, CancellationToken ct = default);

    /// <summary>Borra los eventos del usuario; antes, los avisos y los reportes de otros que apuntan a ellos. La foto se borra en cascada.</summary>
    Task EliminarEventosAsync(int usuarioId, CancellationToken ct = default);

    Task EliminarReportesAsync(int usuarioId, CancellationToken ct = default);
    Task EliminarRecomendacionesAsync(int usuarioId, CancellationToken ct = default);
    Task EliminarLugaresRecuperadosAsync(int usuarioId, CancellationToken ct = default);
    Task EliminarLogsAuditoriaAsync(int usuarioId, CancellationToken ct = default);
    Task EliminarPuentesAsync(int usuarioId, CancellationToken ct = default);
    Task EliminarHorariosAsync(int usuarioId, CancellationToken ct = default);
    Task EliminarHistorialAsync(int usuarioId, CancellationToken ct = default);
    Task EliminarSancionesAsync(int usuarioId, CancellationToken ct = default);

    /// <summary>La baja: conserva <c>correo</c> y <c>google_sub</c> y anonimiza el resto de la fila.</summary>
    Task AnonimizarUsuarioAsync(int usuarioId, string hashInutilizable, DateTime ahoraUtc, CancellationToken ct = default);

    Task EliminarUsuarioAsync(int usuarioId, CancellationToken ct = default);
}
