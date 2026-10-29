using Breaku.Domain;

namespace Breaku.Application.Lugares;

/// <summary>Puerto: lo implementa Infrastructure.</summary>
public interface ILugarRepository
{
    /// <summary>Campus o zona actual del estudiante (HU5), o <c>null</c> si todavía no eligió ninguno o su sede ya no está activa.</summary>
    Task<Zona?> ObtenerZonaDelUsuarioAsync(int usuarioId, CancellationToken ct = default);

    /// <summary>Lugares del catálogo en estado ACTIVO (aprobados por el administrador) y recomendables, con su categoría. Los puntos de referencia no se incluyen.</summary>
    Task<List<Lugar>> ListarActivosAsync(CancellationToken ct = default);
}
