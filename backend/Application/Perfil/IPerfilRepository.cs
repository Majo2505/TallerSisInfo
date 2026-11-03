using Breaku.Domain;

namespace Breaku.Application.Perfil;

public interface IPerfilRepository
{
    /// <summary>Usuario activo con seguimiento de cambios, o <c>null</c>.</summary>
    Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default);

    /// <summary>Guarda los cambios del usuario y deja el antes y el después en historial_cambio, en una transacción.</summary>
    Task GuardarConHistorialAsync(int usuarioId, string valoresAnteriores, string valoresNuevos, CancellationToken ct = default);
}
