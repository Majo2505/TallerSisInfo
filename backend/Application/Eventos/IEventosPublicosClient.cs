namespace Breaku.Application.Eventos;

public interface IEventosPublicosClient
{
    Task<IReadOnlyList<EventoImportado>> ObtenerAsync(CancellationToken ct = default);
}
