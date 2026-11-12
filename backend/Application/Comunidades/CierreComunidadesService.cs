namespace Breaku.Application.Comunidades;

public interface ICierreComunidadesService
{
    Task<IReadOnlyList<int>> ArchivarVencidasAsync(DateTime ahoraUtc, CancellationToken ct = default);

    /// <summary>Al arrancar: apaga los avisos que quedaron de comunidades archivadas antes (por ejemplo, con código anterior).</summary>
    Task<int> LimpiarAvisosDeArchivadasAsync(CancellationToken ct = default);
}

/// <summary>
/// HU15: al vencer el bloque, la comunidad se archiva (deja de verse en Descubrir y ya no admite respuestas) y sus avisos
/// se apagan: una invitación a algo que ya terminó no debe seguir sin leer en la campanita.
/// </summary>
public sealed class CierreComunidadesService : ICierreComunidadesService
{
    private readonly IComunidadRepository _repo;

    public CierreComunidadesService(IComunidadRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<int>> ArchivarVencidasAsync(DateTime ahoraUtc, CancellationToken ct = default)
    {
        var archivadas = await _repo.ArchivarVencidasAsync(ahoraUtc, ct);
        if (archivadas.Count > 0)
            await _repo.DesactivarAvisosDeArchivadasAsync(archivadas, ct);
        return archivadas;
    }

    public Task<int> LimpiarAvisosDeArchivadasAsync(CancellationToken ct = default)
        => _repo.DesactivarAvisosDeArchivadasAsync(null, ct);
}
