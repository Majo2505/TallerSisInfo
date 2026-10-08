using Breaku.Domain;

namespace Breaku.Application.Puentes;

/// <summary>Puerto: lo implementa Infrastructure.</summary>
public interface IPuenteRepository
{
    Task<List<Puente>> ListarPorUsuarioAsync(int usuarioId, CancellationToken ct = default);

    /// <summary>Reemplaza TODOS los puentes del usuario por los nuevos, de forma atómica.</summary>
    Task ReemplazarAsync(int usuarioId, IReadOnlyList<Puente> nuevos, CancellationToken ct = default);
}