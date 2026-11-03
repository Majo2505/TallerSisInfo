using Breaku.Domain;

namespace Breaku.Application.Perfil;

public interface IPerfilRepository
{
    Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default);

    Task GuardarConHistorialAsync(int usuarioId, string valoresAnteriores, string valoresNuevos, CancellationToken ct = default);
}
