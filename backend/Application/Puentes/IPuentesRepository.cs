using Breaku.Domain;

namespace Breaku.Application.Puentes;

public interface IPuenteRepository
{
    Task<List<Puente>> ListarPorUsuarioAsync(int usuarioId, CancellationToken ct = default);

    Task ReemplazarAsync(int usuarioId, IReadOnlyList<Puente> nuevos, CancellationToken ct = default);
}