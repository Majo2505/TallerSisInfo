using Breaku.Domain;

namespace Breaku.Application.Matching;

public record CompaneroCandidato(int UsuarioId, string Nombre, string? Carrera, byte? Semestre, List<Puente> Puentes);

public interface IMatchingRepository
{
    Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default);

    Task<List<CompaneroCandidato>> ListarCandidatosAsync(int usuarioId, int zonaId, Puente puente, CancellationToken ct = default);

    Task GuardarConHistorialAsync(int usuarioId, string valoresAnteriores, string valoresNuevos, CancellationToken ct = default);
}
