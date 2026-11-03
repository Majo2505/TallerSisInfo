using Breaku.Application.Horarios;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Comunidades;

/// <summary>
/// Caso de uso de HU13: creación automática de una comunidad efímera y notificación. Parte de una actividad que HU11 ya
/// entendió, busca a los compañeros de la sede que están libres a esa hora (las mismas reglas y el mismo consentimiento
/// que el matching de HU12) y, solo si encuentra coincidencias, crea la comunidad, invita a los más compatibles (uno por
/// plaza) y le envía a cada uno un aviso (HU4). Si nadie está libre, no crea nada.
/// </summary>
public class ComunidadService
{
    public const string EstadoCreada = "CREADA";
    public const string EstadoSinCoincidencias = "SIN_COINCIDENCIAS";
    public const string EstadoSinZona = "SIN_ZONA";
    public const string EstadoNoVisible = "NO_VISIBLE";

    private readonly IComunidadRepository _repo;
    private readonly TimeProvider _reloj;

    public ComunidadService(IComunidadRepository repo) : this(repo, TimeProvider.System) { }

    public ComunidadService(IComunidadRepository repo, TimeProvider reloj)
    {
        _repo = repo;
        _reloj = reloj;
    }

    public async Task<ResultadoComunidadDto> CrearAsync(int usuarioId, CrearComunidadRequest req, CancellationToken ct = default)
    {
        var ahoraUtc = _reloj.GetUtcNow().UtcDateTime;
        var ahoraLocal = ReglasNotificacion.AHoraLocal(ahoraUtc);

        // 1. Los datos llegan del cliente: se vuelven a validar con las mismas reglas de HU11.
        var datos = Validar(req, ahoraLocal);
        var consultaId = req.ConsultaId!.Value;
        var hora = datos.HoraInicio!.Value;
        var duracion = datos.DuracionMin!.Value;
        var maximo = datos.MaxParticipantes!.Value;

        var usuario = await _repo.ObtenerUsuarioAsync(usuarioId, ct)
                      ?? throw new ReglaNegocioException("NO_ENCONTRADO", "No encontramos tu cuenta.");

        // 2. La extracción tiene que ser suya, de hoy y completa, y no haber creado ya una comunidad.
        var log = await _repo.ObtenerExtraccionAsync(consultaId, ct);
        if (log is null || log.UsuarioId != usuarioId || log.Tipo != LogAuditoriaRag.TipoExtraccionComunidad
            || log.Estado != LogAuditoriaRag.EstadoOk || ReglasNotificacion.AHoraLocal(log.CreatedAt).Date != ahoraLocal.Date)
            throw new ReglaNegocioException("NO_ENCONTRADO", "No encontramos esa actividad. Vuelve a escribirla.");
        if (await _repo.ExisteComunidadDeConsultaAsync(consultaId, ct))
            throw new ReglaNegocioException("YA_CREADA", "Ya creaste una comunidad con esta actividad.");

        // 3. Mismo consentimiento que HU12: sin campus no hay con quién cruzar, y quien no aparece tampoco invita.
        if (usuario.ZonaActualId is not int zonaId || usuario.ZonaActual is not { Activa: true })
            return new ResultadoComunidadDto(EstadoSinZona, ReglasComunidad.MensajeSinZona, null);
        if (!usuario.VisibleMatching)
            return new ResultadoComunidadDto(EstadoNoVisible, ReglasComunidad.MensajeNoVisible, null);

        var inicioLocal = ahoraLocal.Date + hora.ToTimeSpan();
        var inicioUtc = ReglasComunidad.AUtc(inicioLocal);
        var finUtc = inicioUtc.AddMinutes(duracion);
        var actividad = new ReglasMatching.Franja(ReglasComunidad.DiaSemana(inicioLocal), hora, hora.AddMinutes(duracion));

        if (await _repo.EstaEnOtraComunidadAsync(usuarioId, inicioUtc, finUtc, ct))
            throw new ReglaNegocioException("EN_OTRA_COMUNIDAD", "Ya estás en una comunidad a esa hora.");

        // 4. Cruce automático con los puentes de los compañeros.
        var candidatos = await _repo.ListarCandidatosAsync(usuarioId, zonaId, actividad, inicioUtc, finUtc, ct);
        var porId = candidatos.ToDictionary(c => c.UsuarioId);
        var puentes = candidatos.SelectMany(c => c.Puentes.Select(p =>
            new ReglasMatching.PuenteDe(c.UsuarioId, new ReglasMatching.Franja(p.DiaSemana, p.HoraInicio, p.HoraFin))));
        var invitados = ReglasComunidad.ElegirInvitados(actividad, maximo, puentes);

        if (invitados.Count == 0)
            return new ResultadoComunidadDto(EstadoSinCoincidencias, ReglasComunidad.MensajeSinCoincidencias, null);

        // 5. Hay coincidencias: se crea la comunidad, se invita y se avisa, todo junto.
        var comunidad = new ComunidadEfimera
        {
            CreadorId = usuarioId,
            LogId = consultaId,
            TextoOriginal = TextoOriginal(req.Texto, datos.Actividad!),
            Actividad = datos.Actividad!,
            Inicio = inicioUtc,
            Fin = finUtc,
            MaxParticipantes = (short)maximo,
            Estado = ComunidadEfimera.EstadoActiva,
            CreatedAt = ahoraUtc,
            UpdatedAt = ahoraUtc
        };

        // Quien la crea ya está dentro: CONFIRMADO, con respondido_en (ck_part_respuesta).
        var participaciones = new List<Participacion>
        {
            new() { UsuarioId = usuarioId, Estado = Participacion.EstadoConfirmado, InvitadoEn = ahoraUtc, RespondidoEn = ahoraUtc }
        };
        participaciones.AddRange(invitados.Select(i =>
            new Participacion { UsuarioId = i.UsuarioId, Estado = Participacion.EstadoPendiente, InvitadoEn = ahoraUtc }));

        var mensaje = ReglasComunidad.MensajeInvitacion(usuario.Nombre, comunidad.Actividad, actividad.Inicio, actividad.Fin);
        var avisos = invitados
            .Where(i => porId[i.UsuarioId].NotificacionesActivas)   // HU4: quien apagó los avisos no los recibe
            .Select(i => new Notificacion
            {
                UsuarioId = i.UsuarioId,
                Tipo = Notificacion.TipoInvitacionComunidad,
                Mensaje = mensaje,
                CreatedAt = ahoraUtc
            })
            .ToList();

        await _repo.CrearAsync(comunidad, participaciones, avisos, ct);

        var creada = new ComunidadCreadaDto(comunidad.Id, comunidad.Actividad, Hora(actividad.Inicio), Hora(actividad.Fin), maximo,
            invitados.Select(i => new InvitadoDto(porId[i.UsuarioId].Nombre, porId[i.UsuarioId].Carrera,
                Hora(i.EnComun.Inicio), Hora(i.EnComun.Fin), i.MinutosEnComun)).ToList());
        var texto = invitados.Count == 1
            ? "¡Grupo armado! Invitamos a 1 compañero libre a esa hora."
            : $"¡Grupo armado! Invitamos a {invitados.Count} compañeros libres a esa hora.";
        return new ResultadoComunidadDto(EstadoCreada, texto, creada);
    }

    /// <summary>Valida el pedido con <see cref="ReglasExtraccion"/>: hora de hoy entre 07:15 y 21:00, duración y personas.</summary>
    private static DatosActividad Validar(CrearComunidadRequest req, DateTime ahoraLocal)
    {
        if (req.ConsultaId is null || req.Actividad is null)
            throw new ReglaNegocioException("VALIDACION", "Primero describe tu actividad para que la entendamos.");
        if (req.Texto?.Trim().Length > ReglasExtraccion.MaximoLargoTexto)
            throw new ReglaNegocioException("VALIDACION", $"El texto puede tener hasta {ReglasExtraccion.MaximoLargoTexto} caracteres.");

        var a = req.Actividad;
        var hora = ReglasExtraccion.LeerHora(a.HoraInicio);
        if (hora is null)
            throw new ReglaNegocioException("VALIDACION", ReglasExtraccion.PreguntaHora);

        var resultado = ReglasExtraccion.Interpretar(null, new DatosActividad(a.Actividad, hora, a.DuracionMin, a.MaxParticipantes), ahoraLocal);
        if (resultado.Estado != ReglasExtraccion.EstadoCompleto)
            throw new ReglaNegocioException("VALIDACION", resultado.Mensaje ?? ReglasExtraccion.MensajeNoEntendido);
        return resultado.Datos;
    }

    private static string TextoOriginal(string? texto, string actividad)
    {
        var limpio = string.IsNullOrWhiteSpace(texto) ? null : ReglasSeguridadIa.Sanear(texto).Trim();
        return string.IsNullOrEmpty(limpio) ? actividad : limpio;
    }

    private static string Hora(TimeOnly t) => t.ToString("HH:mm");
}
