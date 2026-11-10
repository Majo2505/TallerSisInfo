using Breaku.Application.Horarios;
using Breaku.Domain.Rules;

namespace Breaku.Application.Lugares;

/// <summary>La foto guardada de un lugar.</summary>
public record FotoLugar(byte[] Contenido, string TipoMime, DateTime ActualizadaEn);

/// <summary>Foto subida: la ruta para mostrarla, ya con la versión nueva.</summary>
/// <param name="FotoUrl" example="/api/lugares/12/foto?v=639034123456789010">Ruta relativa a la API.</param>
public record FotoGuardadaDto(string FotoUrl);

public interface IFotoLugarRepository
{
    Task<FotoLugar?> ObtenerAsync(int lugarId, CancellationToken ct = default);

    /// <summary>Guarda o reemplaza la foto. Devuelve false si el lugar no existe.</summary>
    Task<bool> GuardarAsync(int lugarId, byte[] contenido, string tipoMime, DateTime version, int adminId, CancellationToken ct = default);

    /// <summary>Quita la foto (si tenía). Devuelve false si el lugar no existe.</summary>
    Task<bool> EliminarAsync(int lugarId, int adminId, CancellationToken ct = default);
}

public class FotoLugarService
{
    private readonly IFotoLugarRepository _repo;
    private readonly Func<DateTime> _ahora;

    public FotoLugarService(IFotoLugarRepository repo) : this(repo, () => DateTime.UtcNow) { }

    public FotoLugarService(IFotoLugarRepository repo, Func<DateTime> ahora)
    {
        _repo = repo;
        _ahora = ahora;
    }

    public async Task<FotoLugar> ObtenerAsync(int lugarId, CancellationToken ct = default)
        => await _repo.ObtenerAsync(lugarId, ct) ?? throw new ReglaNegocioException("NO_ENCONTRADO", ReglasFotoLugar.MensajeSinFoto);

    public async Task<FotoGuardadaDto> SubirAsync(int lugarId, byte[]? contenido, int adminId, CancellationToken ct = default)
    {
        var (tipo, error) = ReglasFotoLugar.Validar(contenido);
        if (error is not null) throw new ReglaNegocioException("VALIDACION", error);

        var version = ReglasFotoLugar.Version(_ahora());
        if (!await _repo.GuardarAsync(lugarId, contenido!, tipo!, version, adminId, ct))
            throw new ReglaNegocioException("NO_ENCONTRADO", "El lugar no existe.");

        return new FotoGuardadaDto(ReglasFotoLugar.Url(lugarId, version)!);
    }

    public async Task EliminarAsync(int lugarId, int adminId, CancellationToken ct = default)
    {
        if (!await _repo.EliminarAsync(lugarId, adminId, ct))
            throw new ReglaNegocioException("NO_ENCONTRADO", "El lugar no existe.");
    }
}
