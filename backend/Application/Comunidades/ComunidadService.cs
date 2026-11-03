using Breaku.Application.Horarios;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Comunidades;

public class ComunidadService : IInvitadorComunidad
{
    public const string EstadoInvitados = "INVITADOS";
    public const string EstadoSinCoincidencias = "SIN_COINCIDENCIAS";
    public const string EstadoSinZona = "SIN_ZONA";
    public const string EstadoNoVisible = "NO_VISIBLE";
    public const string EstadoCompleta = "COMPLETA";
    public const string EstadoError = "ERROR";

    private readonly IComunidadRepository _repo;
    private readonly TimeProvider _reloj;

    public ComunidadService(IComunidadRepository repo) : this(repo, TimeProvider.System) { }

    public ComunidadService(IComunidadRepository repo, TimeProvider reloj)
    {
        _repo = repo;
        _reloj = reloj;
    }

    public async Task<InvitacionDto> InvitarDeNuevoAsync(int usuarioId, int comunidadId, CancellationToken ct = default)
    {
        var comunidad = await _repo.ObtenerComunidadAsync(comunidadId, ct);
        if (comunidad is null || comunidad.CreadorId != usuarioId)
            throw new ReglaNegocioException("NO_ENCONTRADO", "La actividad no existe.");
        if (comunidad.Estado != ComunidadEfimera.EstadoActiva || comunidad.Fin <= _reloj.GetUtcNow().UtcDateTime)
            throw new ReglaNegocioException("VALIDACION", "La actividad ya terminó.");

        return await InvitarAsync(comunidad, ct);
    }

    public async Task<InvitacionDto> InvitarAsync(ComunidadEfimera comunidad, CancellationToken ct = default)
    {
        var ahoraUtc = _reloj.GetUtcNow().UtcDateTime;
        var creador = await _repo.ObtenerUsuarioAsync(comunidad.CreadorId, ct)
                      ?? throw new ReglaNegocioException("NO_ENCONTRADO", "No encontramos tu cuenta.");
        var yaDentro = await _repo.ListarParticipantesAsync(comunidad.Id, ct);

        var participaciones = new List<Participacion>();
        if (!yaDentro.Contains(comunidad.CreadorId))
            participaciones.Add(new Participacion
            {
                ComunidadId = comunidad.Id, UsuarioId = comunidad.CreadorId, Estado = Participacion.EstadoConfirmado,
                InvitadoEn = ahoraUtc, RespondidoEn = ahoraUtc
            });

        InvitacionDto? sinInvitar = null;
        if (creador.ZonaActualId is null || creador.ZonaActual is not { Activa: true })
            sinInvitar = Vacia(EstadoSinZona, ReglasComunidad.MensajeSinZona);
        else if (!creador.VisibleMatching)
            sinInvitar = Vacia(EstadoNoVisible, ReglasComunidad.MensajeNoVisible);

        var plazas = comunidad.MaxParticipantes - yaDentro.Union([comunidad.CreadorId]).Count();
        if (sinInvitar is null && plazas <= 0)
            sinInvitar = Vacia(EstadoCompleta, ReglasComunidad.MensajeCompleta);

        if (sinInvitar is not null)
        {
            await _repo.GuardarInvitacionesAsync(participaciones, [], ct);
            return sinInvitar;
        }

        var inicioLocal = ReglasNotificacion.AHoraLocal(comunidad.Inicio);
        var finLocal = ReglasNotificacion.AHoraLocal(comunidad.Fin);
        var actividad = new ReglasMatching.Franja(ReglasComunidad.DiaSemana(inicioLocal),
            TimeOnly.FromDateTime(inicioLocal), TimeOnly.FromDateTime(finLocal));

        var candidatos = await _repo.ListarCandidatosAsync(comunidad, creador.ZonaActualId!.Value, actividad, ct);
        var porId = candidatos.ToDictionary(c => c.UsuarioId);
        var puentes = candidatos.SelectMany(c => c.Puentes.Select(p =>
            new ReglasMatching.PuenteDe(c.UsuarioId, new ReglasMatching.Franja(p.DiaSemana, p.HoraInicio, p.HoraFin))));
        var invitados = ReglasComunidad.ElegirInvitados(actividad, plazas, puentes);

        participaciones.AddRange(invitados.Select(i => new Participacion
        {
            ComunidadId = comunidad.Id, UsuarioId = i.UsuarioId, Estado = Participacion.EstadoPendiente, InvitadoEn = ahoraUtc
        }));

        var mensaje = ReglasComunidad.MensajeInvitacion(creador.Nombre, comunidad.Actividad, actividad.Inicio, actividad.Fin);
        var avisos = invitados
            .Where(i => porId[i.UsuarioId].NotificacionesActivas)
            .Select(i => new Notificacion
            {
                UsuarioId = i.UsuarioId,
                Tipo = Notificacion.TipoInvitacionComunidad,
                ComunidadId = comunidad.Id,
                Mensaje = mensaje,
                CreatedAt = ahoraUtc
            })
            .ToList();

        await _repo.GuardarInvitacionesAsync(participaciones, avisos, ct);

        if (invitados.Count == 0)
            return Vacia(EstadoSinCoincidencias, ReglasComunidad.MensajeSinCoincidencias);

        var lista = invitados.Select(i => new InvitadoDto(porId[i.UsuarioId].Nombre, porId[i.UsuarioId].Carrera,
            Hora(i.EnComun.Inicio), Hora(i.EnComun.Fin), i.MinutosEnComun)).ToList();
        return new InvitacionDto(EstadoInvitados, ReglasComunidad.MensajeInvitados(lista.Count), lista);
    }

    private static InvitacionDto Vacia(string estado, string mensaje) => new(estado, mensaje, []);

    private static string Hora(TimeOnly t) => t.ToString("HH:mm");
}
