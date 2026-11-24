using Breaku.Domain;
using Breaku.Domain.Rules;
using Breaku.Application.Horarios;
using Breaku.Application.Notificaciones;

namespace Breaku.Application.Eventos;

public sealed class EventoService
{
    private readonly IEventoRepository _repository;
    private readonly TimeProvider _reloj;
    private readonly IAvisosAdmin? _avisos;

    public EventoService(IEventoRepository repository) : this(repository, TimeProvider.System) { }

    public EventoService(IEventoRepository repository, TimeProvider reloj, IAvisosAdmin? avisos = null)
    {
        _repository = repository;
        _reloj = reloj;
        _avisos = avisos;
    }

    public async Task<List<EventoLugarDto>> ListarLugaresAsync(CancellationToken ct = default)
        => (await _repository.ListarLugaresDisponiblesAsync(ct))
            .OrderBy(l => l.Nombre)
            .Select(l => new EventoLugarDto(l.Id, l.Nombre))
            .ToList();

    public async Task<EventoDto> CrearAsync(
        int usuarioId,
        bool esAdmin,
        EventoFormularioRequest request,
        byte[]? imagen,
        CancellationToken ct = default)
    {
        var (titulo, descripcion, lugarTexto, asistencia, error) = NormalizarYValidar(request);
        if (error is not null) throw new ReglaNegocioException("VALIDACION", error);
        var (tipoMime, errorImagen) = ValidarImagen(imagen);
        if (errorImagen is not null) throw new ReglaNegocioException("VALIDACION", errorImagen);
        var (origen, errorOrigen) = ReglasEvento.ResolverOrigen(esAdmin, request.Origen);
        if (errorOrigen is not null) throw new ReglaNegocioException("VALIDACION", errorOrigen);

        var lugar = await ResolverLugarAsync(request.LugarId, lugarTexto, ct);
        var lugarCatalogado = lugar is { Estado: ReglasBusquedaLugares.EstadoActivo, Validado: true };
        var nombreLugar = lugarCatalogado ? lugar!.Nombre : lugarTexto;
        if (string.IsNullOrWhiteSpace(nombreLugar))
            throw new ReglaNegocioException("VALIDACION", "Selecciona un lugar o escribe dónde será el evento.");

        var ahora = _reloj.GetUtcNow();
        var duplicado = await BuscarDuplicadoAsync(
            titulo, request.LugarId, nombreLugar, request.Inicio!.Value, request.Fin!.Value, null, usuarioId, ct);
        if (duplicado is not null && duplicado.CreadorId == usuarioId)
            throw new ReglaNegocioException(ReglasEvento.CodigoEventoDuplicado,
                ReglasEvento.MensajeEventoDuplicado(duplicado.Titulo, duplicado.Id));

        var motivoDuplicado = duplicado is not null && !esAdmin
            ? ReglasEvento.MotivoPosibleDuplicado(duplicado.Titulo, request.Inicio.Value)
            : null;
        var avisoDuplicado = duplicado is not null && esAdmin
            ? ReglasEvento.AvisoDuplicadoAdmin(duplicado.Titulo, duplicado.Id)
            : null;

        var evaluacion = ReglasEvento.Evaluar(
            esAdmin,
            lugarCatalogado,
            titulo,
            descripcion,
            nombreLugar,
            request.Inicio!.Value,
            request.Fin!.Value,
            ahora);

        var evento = new Evento
        {
            CreadorId = usuarioId,
            Titulo = titulo,
            Descripcion = descripcion,
            LugarId = lugarCatalogado ? lugar!.Id : null,
            LugarTexto = lugarCatalogado ? null : nombreLugar.Trim(),
            Inicio = request.Inicio.Value.UtcDateTime,
            Fin = request.Fin.Value.UtcDateTime,
            Estado = motivoDuplicado is null ? evaluacion.Estado : Evento.EstadoPendiente,
            Origen = origen!,
            Asistencia = asistencia,
            VisitaMin = request.VisitaMin is int min ? (short)min : null,
            VisitaMax = request.VisitaMax is int max ? (short)max : null,
            Motivo = ReglasEvento.UnirMotivos(evaluacion.Motivo, motivoDuplicado),
            CreatedAt = ahora.UtcDateTime,
            UpdatedAt = ahora.UtcDateTime
        };

        var (desdeUtc, hastaUtc) = ReglasEvento.RangoDiaLocal(ahora);
        var creado = await _repository.CrearAsync(
            evento, imagen, tipoMime, !esAdmin, desdeUtc, hastaUtc, ct);
        if (!creado) throw new ReglaNegocioException("LIMITE_DIARIO", ReglasEvento.MensajeLimiteDiario);
        await AvisarAdminsAsync(evento, ct);

        return ADto(
            new EventoListado(evento, nombreLugar, imagen is null ? null : ReglasEvento.VersionImagen(ahora.UtcDateTime)),
            avisoDuplicado);
    }

    public async Task<List<EventoPublicoDto>> ListarPublicadosAsync(string? rango, CancellationToken ct = default)
    {
        var ahora = _reloj.GetUtcNow();
        var hasta = ReglasEvento.HastaDelRango(rango, ahora)
                    ?? throw new ReglaNegocioException("VALIDACION", ReglasEvento.MensajeRangoInvalido);
        return (await _repository.ListarPublicadosAsync(ahora.UtcDateTime, hasta, ct))
            .OrderBy(e => e.Evento.Inicio)
            .Select(APublicoDto)
            .ToList();
    }

    public async Task<EventoPublicoDto> ObtenerPublicadoAsync(int eventoId, CancellationToken ct = default)
    {
        var evento = await _repository.ObtenerPublicadoAsync(eventoId, _reloj.GetUtcNow().UtcDateTime, ct)
                     ?? throw new ReglaNegocioException("NO_ENCONTRADO", "El evento no existe.");
        return APublicoDto(evento);
    }

    public async Task<List<EventoDto>> ListarMiosAsync(int usuarioId, CancellationToken ct = default)
        => (await _repository.ListarMiosAsync(usuarioId, ct))
            .Select(e => ADto(e))
            .ToList();

    public async Task<EventoDto> ActualizarAsync(
        int eventoId,
        int usuarioId,
        EventoFormularioRequest request,
        byte[]? imagen,
        CancellationToken ct = default)
    {
        var evento = await _repository.ObtenerMioAsync(eventoId, usuarioId, ct)
                     ?? throw new ReglaNegocioException("NO_ENCONTRADO", "El evento no existe.");
        var ahora = _reloj.GetUtcNow();
        if (evento.Estado is not (Evento.EstadoPendiente or Evento.EstadoPublicado))
            throw new ReglaNegocioException("EVENTO_NO_EDITABLE", ReglasEvento.MensajeEventoNoEditable);
        if (evento.Inicio <= ahora.UtcDateTime)
            throw new ReglaNegocioException("EVENTO_INICIADO", ReglasEvento.MensajeInicioPasado);

        var (titulo, descripcion, lugarTexto, asistencia, error) = NormalizarYValidar(request);
        if (error is not null) throw new ReglaNegocioException("VALIDACION", error);
        var (tipoMime, errorImagen) = ValidarImagen(imagen);
        if (errorImagen is not null) throw new ReglaNegocioException("VALIDACION", errorImagen);
        if (request.QuitarFoto && imagen is not null)
            throw new ReglaNegocioException("VALIDACION", ReglasEvento.MensajeFotoNuevaYQuitar);

        var lugar = await ResolverLugarAsync(request.LugarId, lugarTexto, ct);
        var lugarCatalogado = lugar is { Estado: ReglasBusquedaLugares.EstadoActivo, Validado: true };
        var nombreLugar = lugarCatalogado ? lugar!.Nombre : lugarTexto;
        if (string.IsNullOrWhiteSpace(nombreLugar))
            throw new ReglaNegocioException("VALIDACION", "Selecciona un lugar o escribe dónde será el evento.");

        var esAdmin = evento.Origen == Evento.OrigenAdmin;
        var duplicado = await BuscarDuplicadoAsync(
            titulo, request.LugarId, nombreLugar, request.Inicio!.Value, request.Fin!.Value, evento.Id, usuarioId, ct);
        if (duplicado is not null && duplicado.CreadorId == usuarioId)
            throw new ReglaNegocioException(ReglasEvento.CodigoEventoDuplicado,
                ReglasEvento.MensajeEventoDuplicado(duplicado.Titulo, duplicado.Id));

        var motivoDuplicado = duplicado is not null && !esAdmin
            ? ReglasEvento.MotivoPosibleDuplicado(duplicado.Titulo, request.Inicio.Value)
            : null;
        var avisoDuplicado = duplicado is not null && esAdmin
            ? ReglasEvento.AvisoDuplicadoAdmin(duplicado.Titulo, duplicado.Id)
            : null;

        var evaluacion = ReglasEvento.Evaluar(
            esAdmin,
            lugarCatalogado,
            titulo,
            descripcion,
            nombreLugar,
            request.Inicio!.Value,
            request.Fin!.Value,
            ahora);

        evento.Titulo = titulo;
        evento.Descripcion = descripcion;
        evento.LugarId = lugarCatalogado ? lugar!.Id : null;
        evento.LugarTexto = lugarCatalogado ? null : nombreLugar.Trim();
        evento.Inicio = request.Inicio.Value.UtcDateTime;
        evento.Fin = request.Fin.Value.UtcDateTime;
        evento.Asistencia = asistencia;
        evento.VisitaMin = request.VisitaMin is int min ? (short)min : null;
        evento.VisitaMax = request.VisitaMax is int max ? (short)max : null;
        evento.Estado = motivoDuplicado is null ? evaluacion.Estado : Evento.EstadoPendiente;
        evento.Motivo = ReglasEvento.UnirMotivos(evaluacion.Motivo, motivoDuplicado);
        evento.UpdatedAt = ahora.UtcDateTime;

        await _repository.GuardarCambiosAsync(evento, imagen, tipoMime, request.QuitarFoto, ct);
        await AvisarAdminsAsync(evento, ct);
        var actualizada = (await _repository.ListarMiosAsync(usuarioId, ct))
            .First(e => e.Evento.Id == evento.Id);
        return ADto(actualizada, avisoDuplicado);
    }

    public async Task<EventoDto> CancelarAsync(int eventoId, int usuarioId, CancellationToken ct = default)
    {
        var evento = await _repository.ObtenerMioAsync(eventoId, usuarioId, ct)
                     ?? throw new ReglaNegocioException("NO_ENCONTRADO", "El evento no existe.");
        if (evento.Inicio <= _reloj.GetUtcNow().UtcDateTime)
            throw new ReglaNegocioException("EVENTO_INICIADO", ReglasEvento.MensajeInicioPasado);
        if (evento.Estado == Evento.EstadoCancelado)
            throw new ReglaNegocioException("YA_CANCELADO", "El evento ya está cancelado.");

        evento.Estado = Evento.EstadoCancelado;
        evento.Motivo = null;
        evento.UpdatedAt = _reloj.GetUtcNow().UtcDateTime;
        await _repository.GuardarCambiosAsync(evento, null, null, false, ct);
        await AvisarAdminsAsync(evento, ct);

        var actualizado = (await _repository.ListarMiosAsync(usuarioId, ct))
            .First(e => e.Evento.Id == evento.Id);
        return ADto(actualizado);
    }

    public async Task<FotoEvento> ObtenerImagenAsync(int eventoId, int usuarioId, bool esAdmin, CancellationToken ct = default)
    {
        var foto = await _repository.ObtenerImagenAsync(eventoId, ct)
                   ?? throw new ReglaNegocioException("NO_ENCONTRADO", "El evento no tiene foto.");
        if (!esAdmin && foto.CreadorId != usuarioId
            && (foto.Estado != Evento.EstadoPublicado || foto.CreadorSuspendido))
            throw new ReglaNegocioException("NO_ENCONTRADO", "El evento no tiene foto.");
        return foto;
    }

    public async Task<List<EventoDto>> ListarPendientesAsync(CancellationToken ct = default)
        => (await _repository.ListarPendientesAsync(_reloj.GetUtcNow().UtcDateTime, ct))
            .Select(e => ADto(e))
            .ToList();

    /// <summary>HU26: aprobar (opcionalmente con cambios) o rechazar un evento PENDIENTE.</summary>
    public async Task ModerarAsync(int eventoId, int adminId, ResolverEventoRequest request, CancellationToken ct = default)
    {
        var estado = request.Estado?.Trim().ToUpperInvariant();
        if (estado is not (Evento.EstadoPublicado or Evento.EstadoRechazado))
            throw new ReglaNegocioException("VALIDACION", "Elige aprobar o rechazar el evento.");
        if (estado == Evento.EstadoRechazado && request.Cambios is not null)
            throw new ReglaNegocioException("VALIDACION", ReglasEvento.MensajeCambiosSoloAlAprobar);

        var motivo = estado == Evento.EstadoRechazado
            ? MotivoObligatorio(request.Motivo, ReglasEvento.MensajeFaltaMotivoRechazo)
            : null;
        var cambios = request.Cambios is null ? null : await ValidarCambiosAsync(request.Cambios, ct);
        var tipo = estado == Evento.EstadoPublicado ? Notificacion.TipoEventoAprobado : Notificacion.TipoEventoRechazado;

        var resultado = await _repository.ModerarAsync(new ModeracionEvento(
            eventoId, adminId, Evento.EstadoPendiente, estado, motivo, tipo, cambios, _reloj.GetUtcNow().UtcDateTime), ct);
        LanzarSiNoSeAplico(resultado, ReglasEvento.MensajeEventoNoPendiente);

        if (_avisos is not null) await _avisos.EventoRevisadoAsync(eventoId, ct);
    }

    /// <summary>HU26: bajar un evento PUBLICADO. Queda RECHAZADO (oculto, no se borra) y el autor recibe EVENTO_BAJADO.</summary>
    public async Task BajarAsync(int eventoId, int adminId, BajarEventoRequest request, CancellationToken ct = default)
    {
        var motivo = MotivoObligatorio(request.Motivo, ReglasEvento.MensajeFaltaMotivoBaja);
        var resultado = await _repository.ModerarAsync(new ModeracionEvento(
            eventoId, adminId, Evento.EstadoPublicado, Evento.EstadoRechazado, motivo, Notificacion.TipoEventoBajado, null,
            _reloj.GetUtcNow().UtcDateTime, request.ReporteId,
            request.ReporteId is null ? null : ReglasEvento.RespuestaReporteBaja(motivo)), ct);
        LanzarSiNoSeAplico(resultado, ReglasEvento.MensajeEventoNoPublicado);

        // HU38: el reporte ya no está pendiente: el aviso «Nuevo reporte» queda leído para todos los admins.
        if (_avisos is not null && request.ReporteId is int reporteId) await _avisos.ReporteRevisadoAsync(reporteId, ct);
    }

    /// <summary>HU26: los eventos PUBLICADOS que todavía no terminaron, para que el admin pueda bajarlos.</summary>
    public async Task<List<EventoDto>> ListarPublicadosAdminAsync(CancellationToken ct = default)
    {
        var ahora = _reloj.GetUtcNow().UtcDateTime;
        return (await _repository.ListarPublicadosAsync(ahora, ahora.AddYears(1), ct))
            .OrderBy(e => e.Evento.Inicio)
            .Select(e => ADto(e))
            .ToList();
    }

    /// <summary>HU26: historial de eventos rechazados o bajados, del más reciente al más antiguo. Solo lectura.</summary>
    public async Task<List<EventoRechazadoDto>> ListarRechazadosAsync(CancellationToken ct = default)
        => (await _repository.ListarRechazadosAsync(ReglasEvento.MaximoHistorialRechazados, ct))
            .Select(r => new EventoRechazadoDto(
                ADto(r.Listado),
                r.FueBajado,
                r.Admin,
                new DateTimeOffset(DateTime.SpecifyKind(r.RechazadoEnUtc ?? r.Listado.Evento.UpdatedAt, DateTimeKind.Utc))
                    .ToOffset(TimeSpan.FromHours(-4))))
            .OrderByDescending(r => r.RechazadoEn)
            .ToList();

    private static string MotivoObligatorio(string? motivo, string mensajeSiFalta)
    {
        var limpio = ReglasSeguridadIa.Sanear(motivo ?? "").Trim();
        if (string.IsNullOrWhiteSpace(limpio)) throw new ReglaNegocioException("VALIDACION", mensajeSiFalta);
        if (limpio.Length > ReglasEvento.MaximoMotivo) throw new ReglaNegocioException("VALIDACION", ReglasEvento.MensajeMotivoLargo);
        return limpio;
    }

    private static void LanzarSiNoSeAplico(ResultadoModeracionEvento resultado, string mensajeEstado)
    {
        switch (resultado)
        {
            case ResultadoModeracionEvento.NoEncontrado:
                throw new ReglaNegocioException("NO_ENCONTRADO", "El evento no existe.");
            case ResultadoModeracionEvento.YaRevisado:
                throw new ReglaNegocioException("YA_REVISADO", mensajeEstado);
            case ResultadoModeracionEvento.Finalizado:
                throw new ReglaNegocioException("EVENTO_FINALIZADO", ReglasEvento.MensajeEventoFinalizado);
            case ResultadoModeracionEvento.ReporteNoCorresponde:
                throw new ReglaNegocioException(ReglasEvento.CodigoReporteNoCorresponde, ReglasEvento.MensajeReporteNoCorresponde);
            case ResultadoModeracionEvento.ReporteYaRevisado:
                throw new ReglaNegocioException("YA_REVISADO", ReglasEvento.MensajeReporteYaRevisado);
        }
    }

    /// <summary>Valida lo que el admin corrigió con las mismas reglas del formulario (HU23). La foto no se toca.</summary>
    private async Task<CambiosEvento> ValidarCambiosAsync(EventoFormularioRequest cambios, CancellationToken ct)
    {
        var (titulo, descripcion, lugarTexto, asistencia, error) = NormalizarYValidar(cambios);
        if (error is not null) throw new ReglaNegocioException("VALIDACION", error);

        var lugar = await ResolverLugarAsync(cambios.LugarId, lugarTexto, ct);
        var lugarCatalogado = lugar is { Estado: ReglasBusquedaLugares.EstadoActivo, Validado: true };
        var nombreLugar = lugarCatalogado ? lugar!.Nombre : lugarTexto;
        if (string.IsNullOrWhiteSpace(nombreLugar))
            throw new ReglaNegocioException("VALIDACION", "Selecciona un lugar o escribe dónde será el evento.");

        return new CambiosEvento(
            titulo,
            descripcion,
            lugarCatalogado ? lugar!.Id : null,
            lugarCatalogado ? null : nombreLugar.Trim(),
            cambios.Inicio!.Value.UtcDateTime,
            cambios.Fin!.Value.UtcDateTime,
            asistencia,
            cambios.VisitaMin is int min ? (short)min : null,
            cambios.VisitaMax is int max ? (short)max : null);
    }

    /// <summary>
    /// La campanita de los admins: si el evento quedó pendiente, les llega (o vuelve a llegar, si se editó); si ya no lo
    /// está (se publicó solo o se canceló), el aviso queda leído.
    /// </summary>
    private async Task AvisarAdminsAsync(Evento evento, CancellationToken ct)
    {
        if (_avisos is null) return;
        if (evento.Estado == Evento.EstadoPendiente) await _avisos.EventoPorRevisarAsync(evento.Id, evento.Titulo, evento.Motivo, ct);
        else await _avisos.EventoRevisadoAsync(evento.Id, ct);
    }

    private async Task<Lugar?> ResolverLugarAsync(int? lugarId, string? lugarTexto, CancellationToken ct)
    {
        if (lugarId is null) return null;
        var lugar = await _repository.ObtenerLugarAsync(lugarId.Value, ct);
        if (lugar is null && string.IsNullOrWhiteSpace(lugarTexto))
            throw new ReglaNegocioException("VALIDACION", "El lugar ya no está disponible. Escribe su nombre para enviarlo a revisión.");
        return lugar;
    }

    private static (string Titulo, string? Descripcion, string? Lugar, string Asistencia, string? Error)
        NormalizarYValidar(EventoFormularioRequest request)
    {
        var titulo = ReglasSeguridadIa.Sanear(request.Titulo ?? "").Trim();
        var descripcion = string.IsNullOrWhiteSpace(request.Descripcion)
            ? null
            : ReglasSeguridadIa.Sanear(request.Descripcion).Trim();
        var lugarTexto = string.IsNullOrWhiteSpace(request.LugarTexto)
            ? null
            : ReglasSeguridadIa.Sanear(request.LugarTexto).Trim();
        var asistencia = request.Asistencia?.Trim().ToUpperInvariant() ?? "";
        var error = ReglasEvento.Validar(titulo, descripcion, lugarTexto, request.LugarId,
            request.Inicio, request.Fin, asistencia, request.VisitaMin, request.VisitaMax);
        return (titulo, descripcion, lugarTexto, asistencia, error);
    }

    private static (string? TipoMime, string? Error) ValidarImagen(byte[]? imagen)
        => imagen is null ? (null, null) : ReglasEvento.ValidarImagen(imagen);

    private async Task<ReglasEvento.CandidatoDuplicado?> BuscarDuplicadoAsync(
        string titulo,
        int? lugarId,
        string lugarTexto,
        DateTimeOffset inicio,
        DateTimeOffset fin,
        int? idActual,
        int creadorId,
        CancellationToken ct)
    {
        var desde = inicio.UtcDateTime.AddDays(-ReglasEvento.MargenBusquedaDuplicadosDias);
        var hasta = fin.UtcDateTime.AddDays(ReglasEvento.MargenBusquedaDuplicadosDias);
        var candidatos = await _repository.BuscarCandidatosDuplicadoAsync(desde, hasta, ct);
        // Se evalúan todos los que coinciden: los propios mandan sobre los ajenos (sin importar el orden de la lista).
        var coincidentes = candidatos.Where(c => ReglasEvento.EsDuplicado(
            c, titulo, lugarId, lugarTexto, inicio, fin, idActual));
        return ReglasEvento.ElegirDuplicado(coincidentes, creadorId, inicio);
    }

    private static EventoPublicoDto APublicoDto(EventoListado item)
    {
        var evento = item.Evento;
        var offsetBolivia = TimeSpan.FromHours(-4);
        static DateTimeOffset Local(DateTime utc, TimeSpan offset)
            => new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToOffset(offset);

        return new EventoPublicoDto(
            evento.Id,
            evento.Titulo,
            evento.Descripcion,
            Local(evento.Inicio, offsetBolivia),
            Local(evento.Fin, offsetBolivia),
            evento.LugarId,
            item.NombreLugar,
            evento.LugarId is null,
            evento.Asistencia,
            evento.VisitaMin,
            evento.VisitaMax,
            ReglasEvento.UrlImagen(evento.Id, item.FotoActualizadaEn));
    }

    private static EventoDto ADto(EventoListado item, string? aviso = null)
    {
        var evento = item.Evento;
        var offsetBolivia = TimeSpan.FromHours(-4);
        static DateTimeOffset Local(DateTime utc, TimeSpan offset)
            => new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToOffset(offset);

        return new EventoDto(
            evento.Id,
            evento.Titulo,
            evento.Descripcion,
            Local(evento.Inicio, offsetBolivia),
            Local(evento.Fin, offsetBolivia),
            evento.LugarId,
            item.NombreLugar,
            evento.Estado,
            evento.Origen,
            evento.Asistencia,
            evento.VisitaMin,
            evento.VisitaMax,
            evento.Motivo,
            ReglasEvento.UrlImagen(evento.Id, item.FotoActualizadaEn),
            Local(evento.CreatedAt, offsetBolivia),
            aviso);
    }
}
