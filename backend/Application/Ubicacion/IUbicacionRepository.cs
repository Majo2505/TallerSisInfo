using Breaku.Domain;

namespace Breaku.Application.Ubicacion;

public interface IUbicacionRepository
{
    Task<List<Zona>> ListarZonasAsync(CancellationToken ct = default);
    Task<Zona?> ObtenerZonaAsync(int zonaId, CancellationToken ct = default);
    Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}
