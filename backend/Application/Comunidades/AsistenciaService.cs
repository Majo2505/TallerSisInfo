using Breaku.Application.Horarios;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Comunidades;

public record ResponderAsistenciaDto(bool? Asisto);

public record AsistenciaDto(int ComunidadId, string MiEstado, int Confirmados, int MaxParticipantes, string Mensaje);

public record ParticipanteDto(string Nombre, string? Carrera, string Estado, bool EsCreador, bool EsTu);

public record ParticipantesDto(
    int ComunidadId, string Actividad, string HoraInicio, string HoraFin, int Confirmados, int MaxParticipantes, bool EsMia,
    List<ParticipanteDto> Participantes);

public record CruceAsistencia(string Actividad, DateTime InicioUtc, DateTime FinUtc);

public record ContextoAsistencia(
    ComunidadEfimera? Comunidad,
    string? MiEstado,
    bool PuedeVerla,
    int Confirmados,
    CruceAsistencia? Cruce,
    List<int> PendientesConAviso);

public record CambioAsistencia(string Estado, bool Guardar, IReadOnlyList<int> AvisarCerrada, string? MensajeCerrada, bool Reabrir);

public record FilaParticipante(int UsuarioId, string Nombre, string? Carrera, string Estado, DateTime? RespondidoEn);

public record ParticipantesComunidad(ComunidadEfimera Comunidad, string NombreCreador, List<FilaParticipante> Filas);

public interface IAsistenciaRepository
{
    Task ResponderAsync(int comunidadId, int usuarioId, DateTime ahoraUtc, Func<ContextoAsistencia, CambioAsistencia> decidir,
        CancellationToken ct = default);

    Task<ParticipantesComunidad?> ObtenerParticipantesAsync(int comunidadId, CancellationToken ct = default);
}

public class AsistenciaService
{
    private readonly IAsistenciaRepository _repo;
    private readonly TimeProvider _reloj;

    public AsistenciaService(IAsistenciaRepository repo) : this(repo, TimeProvider.System) { }

    public AsistenciaService(IAsistenciaRepository repo, TimeProvider reloj)
    {
        _repo = repo;
        _reloj = reloj;
    }

    public async Task<AsistenciaDto> ResponderAsync(int usuarioId, int comunidadId, bool? asisto, CancellationToken ct = default)
    {
        if (asisto is null)
            throw new ReglaNegocioException(ReglasAsistencia.ErrorValidacion, ReglasAsistencia.MensajeFaltaRespuesta);

        var ahoraUtc = _reloj.GetUtcNow().UtcDateTime;
        AsistenciaDto? respuesta = null;

        await _repo.ResponderAsync(comunidadId, usuarioId, ahoraUtc, ctx =>
        {
            var c = ctx.Comunidad;
            var situacion = new ReglasAsistencia.Situacion(
                Existe: c is { Estado: ComunidadEfimera.EstadoActiva },
                EsCreador: c is not null && c.CreadorId == usuarioId,
                Terminada: c is not null && c.Fin <= ahoraUtc,
                MiEstado: ctx.MiEstado,
                PuedeVerla: ctx.PuedeVerla,
                Confirmados: ctx.Confirmados,
                Maximo: c?.MaxParticipantes ?? 0,
                HayCruce: ctx.Cruce is not null);

            var decision = ReglasAsistencia.Decidir(situacion, asisto.Value);
            if (decision.Error == ReglasAsistencia.ErrorCruce)
                throw new ReglaNegocioException(decision.Error, ReglasAsistencia.MensajeCruce(
                    ctx.Cruce!.Actividad, Local(ctx.Cruce.InicioUtc), Local(ctx.Cruce.FinUtc)));
            if (decision.Error is not null)
                throw new ReglaNegocioException(decision.Error, decision.Mensaje ?? "");

            var comunidad = c!;
            var despues = ReglasAsistencia.ConfirmadosDespues(ctx.Confirmados, ctx.MiEstado, decision.Estado);
            var seLlena = decision.Cambia && ReglasAsistencia.SeLlena(ctx.Confirmados, despues, comunidad.MaxParticipantes);
            var seReabre = decision.Cambia && ReglasAsistencia.SeReabre(ctx.Confirmados, despues, comunidad.MaxParticipantes);

            var mensaje = decision.Estado == Participacion.EstadoConfirmado
                ? ReglasAsistencia.MensajeConfirmado(comunidad.Actividad, Local(comunidad.Inicio), Local(comunidad.Fin))
                : ReglasAsistencia.MensajeRechazado;
            respuesta = new AsistenciaDto(comunidad.Id, decision.Estado, despues, comunidad.MaxParticipantes, mensaje);

            return new CambioAsistencia(
                decision.Estado,
                decision.Cambia,
                seLlena ? ctx.PendientesConAviso : [],
                seLlena ? ReglasAsistencia.MensajeCerrada(comunidad.Actividad) : null,
                seReabre);
        }, ct);

        return respuesta!;
    }

    public async Task<ParticipantesDto> ListarParticipantesAsync(int usuarioId, int comunidadId, CancellationToken ct = default)
    {
        var datos = await _repo.ObtenerParticipantesAsync(comunidadId, ct);
        var comunidad = datos?.Comunidad;
        var esMia = comunidad?.CreadorId == usuarioId;
        var miFila = datos?.Filas.FirstOrDefault(f => f.UsuarioId == usuarioId);

        if (datos is null || comunidad!.Estado != ComunidadEfimera.EstadoActiva || (!esMia && miFila is null))
            throw new ReglaNegocioException(ReglasAsistencia.ErrorNoEncontrado, ReglasAsistencia.MensajeNoEncontrada);

        var creadorEnTabla = datos.Filas.Any(f => f.UsuarioId == comunidad.CreadorId);
        var filas = datos.Filas.Where(f => f.UsuarioId != comunidad.CreadorId).ToList();
        var confirmados = ReglasDescubrir.Confirmados(
            datos.Filas.Count(f => f.Estado == Participacion.EstadoConfirmado), creadorEnTabla);

        string NombreVisible(FilaParticipante f)
        {
            if (f.UsuarioId == usuarioId) return "Tú";
            return esMia ? f.Nombre : ReglasDescubrir.NombreCorto(f.Nombre);
        }

        var visibles = esMia
            ? filas
            : filas.Where(f => f.Estado == Participacion.EstadoConfirmado || f.UsuarioId == usuarioId).ToList();

        var lista = new List<ParticipanteDto>
        {
            new(esMia ? "Tú" : ReglasDescubrir.NombreCorto(datos.NombreCreador), null,
                Participacion.EstadoConfirmado, EsCreador: true, EsTu: esMia)
        };
        lista.AddRange(visibles
            .OrderBy(f => ReglasAsistencia.Orden(f.Estado))
            .ThenBy(f => f.RespondidoEn ?? DateTime.MaxValue)
            .ThenBy(f => f.Nombre, StringComparer.CurrentCulture)
            .Select(f => new ParticipanteDto(
                NombreVisible(f),
                esMia ? f.Carrera : null,
                f.Estado,
                EsCreador: false,
                EsTu: f.UsuarioId == usuarioId)));

        return new ParticipantesDto(
            comunidad.Id, comunidad.Actividad, Local(comunidad.Inicio).ToString("HH:mm"), Local(comunidad.Fin).ToString("HH:mm"),
            confirmados, comunidad.MaxParticipantes, esMia, lista);
    }

    private static TimeOnly Local(DateTime utc) => TimeOnly.FromDateTime(ReglasNotificacion.AHoraLocal(utc));
}
