namespace Breaku.Application.Comunidades;

public interface ICierreComunidadesService
{
    Task<IReadOnlyList<int>> ArchivarVencidasAsync(DateTime ahoraUtc, CancellationToken ct = default);
}

public sealed class CierreComunidadesService : ICierreComunidadesService
{
    private readonly IComunidadRepository _repo;

    public CierreComunidadesService(IComunidadRepository repo) => _repo = repo;

    public Task<IReadOnlyList<int>> ArchivarVencidasAsync(DateTime ahoraUtc, CancellationToken ct = default)
        => _repo.ArchivarVencidasAsync(ahoraUtc, ct);
}
