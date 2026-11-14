using Breaku.Domain;

namespace Breaku.Application.Lugares;

public interface ILugarRepository
{
    Task<Zona?> ObtenerZonaDelUsuarioAsync(int usuarioId, CancellationToken ct = default);

    Task<List<Lugar>> ListarActivosAsync(CancellationToken ct = default);

    /// <summary>Los votos («me gusta», «no me sirve») de las recomendaciones de esos lugares, en una sola consulta.</summary>
    Task<Dictionary<int, (int MeGusta, int NoMeSirve)>> ContarVotosAsync(IReadOnlyCollection<int> lugarIds, CancellationToken ct = default);
}
