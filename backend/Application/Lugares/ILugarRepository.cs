using Breaku.Domain;

namespace Breaku.Application.Lugares;

/// <summary>Puerto: lo implementa Infrastructure.</summary>
public interface ILugarRepository
{
    /// <summary>Campus o zona actual del estudiante (HU5), o <c>null</c> si todavía no eligió ninguno.</summary>
    Task<Zona?> ObtenerZonaDelUsuarioAsync(int usuarioId, CancellationToken ct = default);

    /// <summary>Lugares del catálogo en estado ACTIVO (verificados y aprobados por el administrador), con su categoría.</summary>
    Task<List<Lugar>> ListarActivosAsync(CancellationToken ct = default);
}
