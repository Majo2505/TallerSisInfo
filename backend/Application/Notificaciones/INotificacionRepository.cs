using Breaku.Domain;

namespace Breaku.Application.Notificaciones;

/// <summary>Puerto: lo implementa Infrastructure.</summary>
public interface INotificacionRepository
{
    /// <summary>
    /// Puentes del día que empiezan entre <paramref name="desde"/> y <paramref name="hasta"/> (ambos incluidos),
    /// solo de usuarios activos con las notificaciones activadas y que todavía no tienen aviso para esa fecha.
    /// </summary>
    Task<List<Puente>> ListarPuentesPorAvisarAsync(int diaSemana, DateOnly fecha, TimeOnly desde, TimeOnly hasta, CancellationToken ct = default);

    /// <summary>Guarda el aviso. Devuelve false si ese puente ya tenía aviso (uq_notif_puente).</summary>
    Task<bool> AgregarAsync(Notificacion notificacion, CancellationToken ct = default);

    Task<List<Notificacion>> ListarActivasAsync(int usuarioId, int maximo, CancellationToken ct = default);
    Task<List<Notificacion>> ListarActivasDePuenteAsync(int usuarioId, CancellationToken ct = default);
    Task DesactivarAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default);

    /// <summary>Devuelve false si la notificación no existe o es de otro usuario.</summary>
    Task<bool> MarcarLeidaAsync(long id, int usuarioId, CancellationToken ct = default);

    /// <summary>null si el usuario no existe.</summary>
    Task<bool?> ObtenerPreferenciaAsync(int usuarioId, CancellationToken ct = default);
    Task GuardarPreferenciaAsync(int usuarioId, bool activas, CancellationToken ct = default);
}
