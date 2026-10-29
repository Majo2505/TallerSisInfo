using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Ubicacion;

public sealed class UbicacionService
{
    private readonly IUbicacionRepository _repository;

    public UbicacionService(IUbicacionRepository repository) => _repository = repository;

    public async Task<List<ZonaDto>> ListarZonasAsync(CancellationToken ct = default)
        => (await _repository.ListarZonasAsync(ct))
            .OrderBy(z => z.Nombre)
            .Select(z => new ZonaDto(z.Id, z.Nombre))
            .ToList();

    public async Task<EstadoUbicacionDto> ObtenerEstadoAsync(int usuarioId, CancellationToken ct = default)
    {
        var usuario = await ObtenerUsuarioAsync(usuarioId, ct);
        return AEstado(usuario);
    }

    public async Task<EstadoUbicacionDto> CompartirAsync(
        int usuarioId,
        CompartirUbicacionRequest request,
        CancellationToken ct = default)
    {
        var zonas = await _repository.ListarZonasAsync(ct);
        var zona = zonas
            .Select(z => new { Zona = z, Distancia = ReglasBusquedaLugares.DistanciaMetros(request.Latitud, request.Longitud, z.Latitud, z.Longitud) })
            .Where(z => z.Distancia <= z.Zona.RadioMaxM)
            .OrderBy(z => z.Distancia)
            .Select(z => z.Zona)
            .FirstOrDefault();

        if (zona is null)
        {
            throw new UbicacionFueraDeZonaException(
                "No encontramos un campus cercano. Puedes seleccionar tu campus manualmente.");
        }

        var usuario = await ObtenerUsuarioAsync(usuarioId, ct);
        var ahora = DateTime.UtcNow;
        usuario.ZonaActualId = zona.Id;
        usuario.ConsentimientoUbicacion = true;
        usuario.FechaConsentimiento = ahora;
        usuario.UpdatedAt = ahora;
        usuario.UpdatedBy = usuarioId;
        await _repository.GuardarCambiosAsync(ct);

        return AEstado(usuario, zona);
    }

    public async Task<EstadoUbicacionDto> SeleccionarZonaAsync(
        int usuarioId,
        int zonaId,
        CancellationToken ct = default)
    {
        var zona = await _repository.ObtenerZonaAsync(zonaId, ct)
            ?? throw new KeyNotFoundException("El campus seleccionado no existe.");
        var usuario = await ObtenerUsuarioAsync(usuarioId, ct);
        var ahora = DateTime.UtcNow;
        usuario.ZonaActualId = zona.Id;
        usuario.ConsentimientoUbicacion = false;
        usuario.FechaConsentimiento = null;
        usuario.UpdatedAt = ahora;
        usuario.UpdatedBy = usuarioId;
        await _repository.GuardarCambiosAsync(ct);

        return AEstado(usuario, zona);
    }

    public async Task RevocarAsync(int usuarioId, CancellationToken ct = default)
    {
        var usuario = await ObtenerUsuarioAsync(usuarioId, ct);
        var ahora = DateTime.UtcNow;
        usuario.ZonaActualId = null;
        usuario.ConsentimientoUbicacion = false;
        usuario.FechaConsentimiento = null;
        usuario.UpdatedAt = ahora;
        usuario.UpdatedBy = usuarioId;
        await _repository.GuardarCambiosAsync(ct);
    }

    private async Task<Usuario> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct)
        => await _repository.ObtenerUsuarioAsync(usuarioId, ct)
            ?? throw new KeyNotFoundException("No se encontró el estudiante.");

    private static EstadoUbicacionDto AEstado(Usuario usuario, Zona? zona = null)
        => new(
            usuario.ConsentimientoUbicacion,
            zona is not null
                ? new ZonaDto(zona.Id, zona.Nombre)
                : usuario.ZonaActual is { Activa: true }
                    ? new ZonaDto(usuario.ZonaActual.Id, usuario.ZonaActual.Nombre)
                    : null
        );
}

public sealed class UbicacionFueraDeZonaException(string message) : Exception(message);
