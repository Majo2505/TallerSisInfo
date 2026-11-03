using Breaku.Domain;

namespace Breaku.Application.Notificaciones;

public interface INotificacionRepository
{
    Task<List<Puente>> ListarPuentesPorAvisarAsync(int diaSemana, DateOnly fecha, TimeOnly desde, TimeOnly hasta, CancellationToken ct = default);

    Task<bool> AgregarAsync(Notificacion notificacion, CancellationToken ct = default);

    Task<List<Notificacion>> ListarActivasAsync(int usuarioId, int maximo, CancellationToken ct = default);
    Task<List<Notificacion>> ListarActivasDePuenteAsync(int usuarioId, CancellationToken ct = default);
    Task DesactivarAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default);

    Task<bool> MarcarLeidaAsync(long id, int usuarioId, CancellationToken ct = default);

    Task<bool?> ObtenerPreferenciaAsync(int usuarioId, CancellationToken ct = default);
    Task GuardarPreferenciaAsync(int usuarioId, bool activas, CancellationToken ct = default);
}
