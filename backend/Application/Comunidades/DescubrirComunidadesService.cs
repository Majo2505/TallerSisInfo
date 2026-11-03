using Breaku.Application.Horarios;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Comunidades;

/// <summary>Una comunidad activa tal como la lee la base, antes de armar lo que se muestra.</summary>
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

/// <summary>Puerto: lo implementa Infrastructure.</summary>
public interface IDescubrirComunidadesRepository
{
    /// <summary>Usuario activo con su sede, o <c>null</c>.</summary>
    Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default);

    /// <summary>
    /// Comunidades activas que todavía no terminaron y empiezan antes de <paramref name="hastaUtc"/> (hoy), de la más
    /// próxima a la más lejana. Siempre las que creó el usuario y aquellas en las que participa; con
    /// <paramref name="deOtrosEnZona"/>, también las de estudiantes de esa sede que aceptaron el matching (HU12).
    /// </summary>
    Task<List<ComunidadActiva>> ListarActivasAsync(int usuarioId, int? deOtrosEnZona, DateTime ahoraUtc, DateTime hastaUtc,
        int maximo, CancellationToken ct = default);
}

/// <summary>Comunidad que se ve en Descubrir. Sin ids de personas ni correos: solo lo necesario para decidir.</summary>
/// <param name="Id" example="12">Identificador de la comunidad.</param>
/// <param name="Actividad" example="jugar cartas">Qué se va a hacer.</param>
/// <param name="HoraInicio" example="16:00">Hora de inicio de hoy, hora de Bolivia (<c>HH:mm</c>).</param>
/// <param name="HoraFin" example="17:00">Hora de fin (<c>HH:mm</c>).</param>
/// <param name="MaxParticipantes" example="4">Cupo total, contando a quien la creó.</param>
/// <param name="Confirmados" example="2">Personas que ya están dentro, contando a quien la creó.</param>
/// <param name="Creador" example="Ana P.">Nombre e inicial del apellido de quien la creó, o «Tú».</param>
/// <param name="EsMia" example="false"><c>true</c> si la creó el usuario del token.</param>
/// <param name="MiEstado" example="PENDIENTE">Si al usuario lo invitaron: <c>PENDIENTE</c>, <c>CONFIRMADO</c> o <c>RECHAZADO</c>. <c>null</c> si no está en ella.</param>
public record ComunidadDescubrirDto(
    int Id, string Actividad, string HoraInicio, string HoraFin, int MaxParticipantes, int Confirmados, string Creador, bool EsMia,
    string? MiEstado);

/// <summary>Resultado de Descubrir.</summary>
/// <param name="Estado" example="OK">
/// <c>OK</c>: hay comunidades. <c>SIN_RESULTADOS</c>: hoy no hay ninguna activa en el campus. <c>SIN_ZONA</c>: el estudiante no
/// eligió campus. <c>NO_VISIBLE</c>: no aceptó aparecer en el matching (HU12) y, como el consentimiento es recíproco, solo ve las
/// suyas y aquellas a las que lo invitaron.
/// </param>
/// <param name="Mensaje" example="Hoy todavía no hay comunidades activas en tu campus. ¡Crea la primera!">Texto para mostrar si el estado no es <c>OK</c>.</param>
/// <param name="Comunidades">Las comunidades, de la más próxima a la más lejana.</param>
public record ResultadoDescubrirDto(string Estado, string? Mensaje, List<ComunidadDescubrirDto> Comunidades);

/// <summary>
/// Adición a HU11: Comunidades → Descubrir muestra las actividades reales activas de hoy, incluida la que el estudiante acaba
/// de crear. Solo lectura: unirse o confirmar asistencia es de HU14.
/// </summary>
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

        // «Hoy» es el día de Bolivia: hasta la medianoche local, pasada a UTC como se guardan inicio y fin.
        var ahoraUtc = _reloj.GetUtcNow().UtcDateTime;
        var ahoraLocal = ReglasNotificacion.AHoraLocal(ahoraUtc);
        var finDelDiaUtc = DateTime.SpecifyKind(ahoraLocal.Date.AddDays(1) - ReglasNotificacion.DesfaseBolivia, DateTimeKind.Utc);

        var tieneZona = usuario.ZonaActualId is not null && usuario.ZonaActual is { Activa: true };
        var verDeOtros = tieneZona && usuario.VisibleMatching ? usuario.ZonaActualId : null;
        var filas = await _repo.ListarActivasAsync(usuarioId, verDeOtros, ahoraUtc, finDelDiaUtc, ReglasDescubrir.Maximo, ct);

        var (estado, mensaje) = ReglasDescubrir.Estado(tieneZona, usuario.VisibleMatching, filas.Count);
        return new ResultadoDescubrirDto(estado, mensaje, filas.Select(f => ADto(f, usuarioId)).ToList());
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
