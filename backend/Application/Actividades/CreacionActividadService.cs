using Breaku.Application.Comunidades;
using Breaku.Application.Horarios;
using Breaku.Application.Recomendaciones;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Actividades;

public class CreacionActividadService
{
    private readonly IActividadRepository _repo;
    private readonly TimeProvider _reloj;
    private readonly IInvitadorComunidad? _invitador;

    public CreacionActividadService(IActividadRepository repo) : this(repo, TimeProvider.System) { }

    public CreacionActividadService(IActividadRepository repo, TimeProvider reloj) : this(repo, reloj, null) { }

    public CreacionActividadService(IActividadRepository repo, TimeProvider reloj, IInvitadorComunidad? invitador)
    {
        _repo = repo;
        _reloj = reloj;
        _invitador = invitador;
    }

    public async Task<ActividadCreadaDto> CrearAsync(int usuarioId, CrearActividadRequest req, CancellationToken ct = default)
    {
        var texto = LimpiarTexto(req.Texto);

        var rechazo = ReglasSeguridadIa.Evaluar(texto) ?? ReglasSeguridadIa.Evaluar(req.Actividad);
        if (rechazo is not null)
            throw new ReglaNegocioException(RecomendacionService.CodigoSolicitudNoPermitida, rechazo.Mensaje);

        var extraccion = await _repo.ObtenerExtraccionAsync(req.ConsultaId, usuarioId, ct)
                         ?? throw new ReglaNegocioException("VALIDACION", "La consulta de la actividad no existe.");
        if (extraccion.Estado != LogAuditoriaRag.EstadoOk)
            throw new ReglaNegocioException("VALIDACION", "Esa actividad todavía tiene datos por completar.");
        if (await _repo.ConsultaYaUsadaAsync(req.ConsultaId, ct))
            throw new ReglaNegocioException("DUPLICADO", "Esa actividad ya se creó.");

        var ahoraUtc = _reloj.GetUtcNow().UtcDateTime;
        var ahoraLocal = ReglasNotificacion.AHoraLocal(ahoraUtc);
        var hora = ReglasExtraccion.LeerHora(req.HoraInicio)
                   ?? throw new ReglaNegocioException("VALIDACION", ReglasExtraccion.PreguntaHora);

        var revisada = ReglasExtraccion.Interpretar(null, new DatosActividad(req.Actividad, hora, req.DuracionMin, req.MaxParticipantes), ahoraLocal);
        if (revisada.Estado != ReglasExtraccion.EstadoCompleto)
            throw new ReglaNegocioException("VALIDACION", revisada.Mensaje ?? ReglasExtraccion.MensajeNoEntendido);

        var datos = revisada.Datos;
        var inicioUtc = ahoraLocal.Date.Add(hora.ToTimeSpan()) - ReglasNotificacion.DesfaseBolivia;
        var comunidad = new ComunidadEfimera
        {
            CreadorId = usuarioId,
            LogId = req.ConsultaId,
            TextoOriginal = texto,
            Actividad = datos.Actividad!,
            Inicio = DateTime.SpecifyKind(inicioUtc, DateTimeKind.Utc),
            Fin = DateTime.SpecifyKind(inicioUtc.AddMinutes(datos.DuracionMin!.Value), DateTimeKind.Utc),
            MaxParticipantes = (short)datos.MaxParticipantes!.Value,
            Estado = ComunidadEfimera.EstadoActiva,
            CreatedAt = ahoraUtc,
            UpdatedAt = ahoraUtc
        };
        await _repo.AgregarAsync(comunidad, ct);

        return new ActividadCreadaDto(
            comunidad.Id, comunidad.Actividad, hora.ToString("HH:mm"), hora.AddMinutes(datos.DuracionMin.Value).ToString("HH:mm"),
            datos.DuracionMin.Value, comunidad.MaxParticipantes, comunidad.Estado, await InvitarAsync(comunidad, ct));
    }

    private async Task<InvitacionDto?> InvitarAsync(ComunidadEfimera comunidad, CancellationToken ct)
    {
        if (_invitador is null) return null;
        try
        {
            return await _invitador.InvitarAsync(comunidad, ct);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return new InvitacionDto(ComunidadService.EstadoError,
                "Tu actividad quedó creada, pero no pudimos invitar a tus compañeros. Vuelve a buscar en un momento.", []);
        }
    }

    private static string LimpiarTexto(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            throw new ReglaNegocioException("VALIDACION", "Falta la descripción de la actividad.");

        var limpio = texto.Trim();
        if (limpio.Length > ReglasExtraccion.MaximoLargoTexto)
            throw new ReglaNegocioException("VALIDACION",
                $"La descripción puede tener como máximo {ReglasExtraccion.MaximoLargoTexto} caracteres.");

        var saneado = ReglasSeguridadIa.Sanear(limpio);
        return saneado.Length == 0
            ? throw new ReglaNegocioException("VALIDACION", "Falta la descripción de la actividad.")
            : saneado;
    }
}
