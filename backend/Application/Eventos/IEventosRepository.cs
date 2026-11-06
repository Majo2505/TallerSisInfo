using Breaku.Domain;

namespace Breaku.Application.Eventos;

public interface IEventosRepository
{
    Task UpsertAsync(IReadOnlyList<EventoImportado> eventos, DateTime actualizadoEn, CancellationToken ct = default);
    Task<List<EventoUniversitario>> ListarVigentesAsync(DateTime ahoraUtc, DateTime hastaUtc, CancellationToken ct = default);
}
