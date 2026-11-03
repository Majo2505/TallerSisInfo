using Breaku.Domain;

namespace Breaku.Application.Comunidades;

public interface IComunidadRepository
{
    Task<ComunidadEfimera?> ObtenerAsync(int id, CancellationToken ct = default);
    Task<List<ComunidadEfimera>> ListarActivasAsync(DateTime ahora, CancellationToken ct = default);
    Task<int> ContarParticipantesAsync(int comunidadId, CancellationToken ct = default);
    Task<List<int>> ListarCandidatosAsync(int creadorId, DateTime inicio, DateTime fin, int maximo, CancellationToken ct = default);
    Task<ComunidadEfimera> CrearAsync(ComunidadEfimera comunidad, IReadOnlyCollection<int> usuarios, CancellationToken ct = default);
    Task<bool> ResponderAsync(int comunidadId, int usuarioId, string estado, CancellationToken ct = default);
    Task ArchivarVencidasAsync(DateTime ahora, CancellationToken ct = default);
}
