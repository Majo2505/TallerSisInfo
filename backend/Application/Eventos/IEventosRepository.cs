using Breaku.Domain;

namespace Breaku.Application.Eventos;

public interface IEventosRepository
{
    Task UpsertAsync(IReadOnlyList<EventoImportado> eventos, DateTime actualizadoEn, CancellationToken ct = default);
    Task<List<EventoUniversitario>> ListarProximosAsync(DateTime ahoraUtc, CancellationToken ct = default);
}
