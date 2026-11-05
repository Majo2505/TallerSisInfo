using Breaku.Application.Horarios;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Comunidades;

public record ComunidadActiva(
    int Id,
    int CreadorId,
    string NombreCreador,
    string Actividad,
    DateTime InicioUtc,
    DateTime FinUtc,
    short MaxParticipantes,
    int ConfirmadosEnTabla,
    bool CreadorEnTabla,
    string? MiEstado
);

public interface IDescubrirComunidadesRepository
{
    Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default);

    Task<List<ComunidadActiva>> ListarActivasAsync(int usuarioId, int? deOtrosEnZona, DateTime ahoraUtc, DateTime hastaUtc,
        int maximo, CancellationToken ct = default);

    Task<bool> PuedeVerAsync(int usuarioId, int comunidadId, int? deOtrosEnZona, DateTime ahoraUtc, CancellationToken ct = default);
}

public record ComunidadDescubrirDto(
    int Id, string Actividad, string HoraInicio, string HoraFin, int MaxParticipantes, int Confirmados, string Creador, bool EsMia,
    string? MiEstado);

public record ResultadoDescubrirDto(string Estado, string? Mensaje, List<ComunidadDescubrirDto> Comunidades);

public class DescubrirComunidadesService
{
    private readonly IDescubrirComunidadesRepository _repo;
    private readonly TimeProvider _reloj;

    public DescubrirComunidadesService(IDescubrirComunidadesRepository repo) : this(repo, TimeProvider.System) { }

    public DescubrirComunidadesService(IDescubrirComunidadesRepository repo, TimeProvider reloj)
    {
        _repo = repo;
        _reloj = reloj;
    }

    public async Task<ResultadoDescubrirDto> ListarAsync(int usuarioId, CancellationToken ct = default)
    {
        var usuario = await _repo.ObtenerUsuarioAsync(usuarioId, ct)
                      ?? throw new ReglaNegocioException("NO_ENCONTRADO", "No encontramos tu cuenta.");

        var ahoraUtc = _reloj.GetUtcNow().UtcDateTime;
        var ahoraLocal = ReglasNotificacion.AHoraLocal(ahoraUtc);
        var finDelDiaUtc = DateTime.SpecifyKind(ahoraLocal.Date.AddDays(1) - ReglasNotificacion.DesfaseBolivia, DateTimeKind.Utc);

        var tieneZona = usuario.ZonaActualId is not null && usuario.ZonaActual is { Activa: true };
        var verDeOtros = tieneZona && usuario.VisibleMatching ? usuario.ZonaActualId : null;
        var filas = await _repo.ListarActivasAsync(usuarioId, verDeOtros, ahoraUtc, finDelDiaUtc, ReglasDescubrir.Maximo, ct);

        var (estado, mensaje) = ReglasDescubrir.Estado(tieneZona, usuario.VisibleMatching, filas.Count);
        return new ResultadoDescubrirDto(estado, mensaje, filas.Select(f => ADto(f, usuarioId)).ToList());
    }

    public async Task<bool> PuedeVerAsync(int usuarioId, int comunidadId, CancellationToken ct = default)
    {
        var usuario = await _repo.ObtenerUsuarioAsync(usuarioId, ct);
        if (usuario is null) return false;

        var tieneZona = usuario.ZonaActualId is not null && usuario.ZonaActual is { Activa: true };
        var verDeOtros = tieneZona && usuario.VisibleMatching ? usuario.ZonaActualId : null;
        return await _repo.PuedeVerAsync(usuarioId, comunidadId, verDeOtros, _reloj.GetUtcNow().UtcDateTime, ct);
    }

    private static ComunidadDescubrirDto ADto(ComunidadActiva f, int usuarioId)
    {
        var esMia = f.CreadorId == usuarioId;
        return new ComunidadDescubrirDto(
            f.Id, f.Actividad,
            ReglasNotificacion.AHoraLocal(f.InicioUtc).ToString("HH:mm"),
            ReglasNotificacion.AHoraLocal(f.FinUtc).ToString("HH:mm"),
            f.MaxParticipantes,
            ReglasDescubrir.Confirmados(f.ConfirmadosEnTabla, f.CreadorEnTabla),
            esMia ? "Tú" : ReglasDescubrir.NombreCorto(f.NombreCreador),
            esMia,
            esMia ? null : f.MiEstado);
    }
}
