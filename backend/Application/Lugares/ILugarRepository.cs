using Breaku.Domain;

namespace Breaku.Application.Lugares;

public interface ILugarRepository
{
    Task<Zona?> ObtenerZonaDelUsuarioAsync(int usuarioId, CancellationToken ct = default);

    Task<List<Lugar>> ListarActivosAsync(CancellationToken ct = default);
}
