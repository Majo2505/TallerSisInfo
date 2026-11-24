using Breaku.Application.Eventos;
using Breaku.Application.Horarios;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Tests;

public sealed class EventoServiceTests
{
    private const int Usuario = 7;
    private const int Admin = 2;
    private static readonly DateTimeOffset Ahora = new(2026, 11, 17, 16, 0, 0, TimeSpan.Zero);

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }

    private sealed class RepositorioFalso : IEventoRepository
    {
        public List<Evento> Eventos { get; } = [];
        public List<Lugar> Lugares { get; } = [new Lugar { Id = 3, Nombre = "Biblioteca", Estado = "ACTIVO", Validado = true }];
        public Dictionary<int, (byte[] Contenido, string TipoMime, DateTime Actualizada)> Imagenes { get; } = [];
        public HashSet<int> Suspendidos { get; } = [];
        public int VecesModerado { get; private set; }

        public Task<List<Lugar>> ListarLugaresDisponiblesAsync(CancellationToken ct = default)
            => Task.FromResult(Lugares.ToList());

        public Task<Lugar?> ObtenerLugarAsync(int lugarId, CancellationToken ct = default)
            => Task.FromResult(Lugares.FirstOrDefault(l => l.Id == lugarId));

        public Task<List<EventoParaPuente>> ListarPublicadosHoyAsync(
            DateTime desdeUtc,
            DateTime hastaUtc,
            DateTime ahoraUtc,
            CancellationToken ct = default)
            => Task.FromResult(new List<EventoParaPuente>());

        public Task<bool> CrearAsync(
            Evento evento,
            byte[]? imagen,
            string? tipoMime,
            bool aplicarLimiteDiario,
            DateTime desdeUtc,
            DateTime hastaUtc,
            CancellationToken ct = default)
        {
            if (aplicarLimiteDiario && Eventos.Count(e =>
                    e.CreadorId == evento.CreadorId && e.CreatedAt >= desdeUtc && e.CreatedAt < hastaUtc) >= ReglasEvento.MaximoEventosPorDia)
                return Task.FromResult(false);

            evento.Id = Eventos.Count + 1;
            Eventos.Add(evento);
            if (imagen is not null && tipoMime is not null)
                Imagenes[evento.Id] = (imagen, tipoMime, ReglasEvento.VersionImagen(evento.CreatedAt));
            return Task.FromResult(true);
        }

        public Task<List<ReglasEvento.CandidatoDuplicado>> BuscarCandidatosDuplicadoAsync(
            DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default)
            => Task.FromResult(Eventos
                .Where(e => (e.Estado == Evento.EstadoPublicado || e.Estado == Evento.EstadoPendiente)
                    && e.Fin > desdeUtc
                    && e.Inicio < hastaUtc)
                .Select(e => new ReglasEvento.CandidatoDuplicado(
                    e.Id, e.CreadorId, e.Titulo, e.Inicio, e.Fin, e.LugarId, e.LugarTexto,
                    Lugares.FirstOrDefault(l => l.Id == e.LugarId)?.Nombre, e.Estado))
                .ToList());

        public Task<List<EventoListado>> ListarMiosAsync(int usuarioId, CancellationToken ct = default)
            => Task.FromResult(Eventos.Where(e => e.CreadorId == usuarioId).Select(AListado).ToList());

        public Task<Evento?> ObtenerMioAsync(int eventoId, int usuarioId, CancellationToken ct = default)
            => Task.FromResult(Eventos.FirstOrDefault(e => e.Id == eventoId && e.CreadorId == usuarioId));

        public Task GuardarCambiosAsync(
            Evento evento, byte[]? imagen, string? tipoMime, bool quitarImagen = false, CancellationToken ct = default)
        {
            if (imagen is not null && tipoMime is not null)
                Imagenes[evento.Id] = (imagen, tipoMime, ReglasEvento.VersionImagen(evento.UpdatedAt));
            else if (quitarImagen)
                Imagenes.Remove(evento.Id);
            return Task.CompletedTask;
        }

        public Task<FotoEvento?> ObtenerImagenAsync(int eventoId, CancellationToken ct = default)
        {
            if (!Imagenes.TryGetValue(eventoId, out var imagen)) return Task.FromResult<FotoEvento?>(null);
            var evento = Eventos.Single(e => e.Id == eventoId);
            return Task.FromResult<FotoEvento?>(new FotoEvento(
                imagen.Contenido, imagen.TipoMime, imagen.Actualizada, evento.CreadorId, evento.Estado,
                Suspendidos.Contains(evento.CreadorId)));
        }

        public Task<List<EventoListado>> ListarPublicadosAsync(DateTime ahoraUtc, DateTime hastaUtc, CancellationToken ct = default)
            => Task.FromResult(Eventos
                .Where(e => e.Estado == Evento.EstadoPublicado
                    && e.Fin > ahoraUtc
                    && e.Inicio < hastaUtc
                    && !Suspendidos.Contains(e.CreadorId))
                .Select(AListado).ToList());

        public Task<EventoListado?> ObtenerPublicadoAsync(int eventoId, DateTime ahoraUtc, CancellationToken ct = default)
            => Task.FromResult(Eventos
                .Where(e => e.Id == eventoId && e.Estado == Evento.EstadoPublicado && !Suspendidos.Contains(e.CreadorId))
                .Select(AListado).FirstOrDefault());

        public Task<List<EventoListado>> ListarPendientesAsync(DateTime ahoraUtc, CancellationToken ct = default)
            => Task.FromResult(Eventos
                .Where(e => e.Estado == Evento.EstadoPendiente && e.Fin > ahoraUtc)
                .Select(AListado).ToList());

        public List<ModeracionEvento> Moderaciones { get; } = [];
        /// <summary>HU38: los reportes que existen (id, evento al que apuntan, estado) y los que quedaron resueltos con su respuesta.</summary>
        public List<(int Id, int EventoId, string Estado)> Reportes { get; } = [];
        public List<(int Id, string? Respuesta)> ReportesResueltos { get; } = [];

        public Task<List<EventoRechazado>> ListarRechazadosAsync(int maximo, CancellationToken ct = default)
            => Task.FromResult(Eventos
                .Where(e => e.Estado == Evento.EstadoRechazado)
                .OrderByDescending(e => e.UpdatedAt)
                .Take(maximo)
                .Select(e =>
                {
                    var m = Moderaciones.LastOrDefault(x => x.EventoId == e.Id && x.EstadoNuevo == Evento.EstadoRechazado);
                    return new EventoRechazado(AListado(e), m?.EstadoEsperado == Evento.EstadoPublicado,
                        m is null ? null : "Admin", m?.AhoraUtc);
                })
                .ToList());

        public Task<ResultadoModeracionEvento> ModerarAsync(ModeracionEvento m, CancellationToken ct = default)
        {
            var evento = Eventos.FirstOrDefault(e => e.Id == m.EventoId);
            if (evento is null) return Task.FromResult(ResultadoModeracionEvento.NoEncontrado);
            if (evento.Estado != m.EstadoEsperado) return Task.FromResult(ResultadoModeracionEvento.YaRevisado);
            if (evento.Fin <= m.AhoraUtc) return Task.FromResult(ResultadoModeracionEvento.Finalizado);
            if (m.ReporteId is int idReporte)
            {
                var reporte = Reportes.FirstOrDefault(x => x.Id == idReporte);
                if (reporte.Id == 0 || reporte.EventoId != m.EventoId) return Task.FromResult(ResultadoModeracionEvento.ReporteNoCorresponde);
                if (reporte.Estado != "PENDIENTE") return Task.FromResult(ResultadoModeracionEvento.ReporteYaRevisado);
                ReportesResueltos.Add((idReporte, m.RespuestaReporte));
            }
            if (m.Cambios is { } c)
            {
                evento.Titulo = c.Titulo;
                evento.LugarId = c.LugarId;
                evento.LugarTexto = c.LugarTexto;
                evento.Inicio = c.InicioUtc;
                evento.Fin = c.FinUtc;
            }
            evento.Estado = m.EstadoNuevo;
            evento.Motivo = m.EstadoNuevo == Evento.EstadoRechazado ? m.Motivo : null;
            evento.UpdatedAt = m.AhoraUtc;
            VecesModerado++;
            Moderaciones.Add(m);
            return Task.FromResult(ResultadoModeracionEvento.Actualizado);
        }

        private EventoListado AListado(Evento evento)
        {
            var lugar = evento.LugarId is int id
                ? Lugares.First(l => l.Id == id).Nombre
                : evento.LugarTexto!;
            var foto = Imagenes.GetValueOrDefault(evento.Id);
            return new EventoListado(evento, lugar, foto.Actualizada == default ? null : foto.Actualizada);
        }
    }

    private static EventoFormularioRequest Solicitud(
        string titulo = "Feria de ciencias",
        int? lugarId = 3,
        string? lugarTexto = "Biblioteca",
        DateTimeOffset? inicio = null,
        DateTimeOffset? fin = null,
        string? descripcion = null,
        string asistencia = ReglasEvento.AsistenciaFija,
        int? visitaMin = null,
        int? visitaMax = null,
        string? origen = null)
        => new(titulo, descripcion,
            inicio ?? new DateTimeOffset(2026, 11, 17, 13, 0, 0, TimeSpan.FromHours(-4)),
            fin ?? new DateTimeOffset(2026, 11, 17, 14, 0, 0, TimeSpan.FromHours(-4)),
            lugarId, lugarTexto, asistencia, visitaMin, visitaMax, origen);

    private static (EventoService Servicio, RepositorioFalso Repo) CrearServicio()
    {
        var repo = new RepositorioFalso();
        return (new EventoService(repo, new RelojFijo(Ahora)), repo);
    }

    [Fact]
    public async Task Evento_valido_se_publica_y_guarda_solo_el_id_del_lugar()
    {
        var (servicio, repo) = CrearServicio();

        var dto = await servicio.CrearAsync(Usuario, false, Solicitud(), null);

        var evento = Assert.Single(repo.Eventos);
        Assert.Equal(Evento.EstadoPublicado, evento.Estado);
        Assert.Equal(Evento.OrigenFormulario, evento.Origen);
        Assert.Equal(3, evento.LugarId);
        Assert.Null(evento.LugarTexto);
        Assert.Equal("Biblioteca", dto.Lugar);
    }

    [Fact]
    public async Task Un_lugar_fuera_del_catalogo_se_guarda_como_texto_y_queda_pendiente()
    {
        var (servicio, repo) = CrearServicio();

        await servicio.CrearAsync(Usuario, false, Solicitud(lugarId: null, lugarTexto: "Patio lateral"), null);

        var evento = Assert.Single(repo.Eventos);
        Assert.Null(evento.LugarId);
        Assert.Equal("Patio lateral", evento.LugarTexto);
        Assert.Equal(Evento.EstadoPendiente, evento.Estado);
        Assert.Contains(ReglasEvento.MensajeLugarPendiente, evento.Motivo);
    }

    [Fact]
    public async Task Texto_detectado_por_las_reglas_pasa_a_revision_sin_rechazar_la_solicitud()
    {
        var (servicio, repo) = CrearServicio();

        var dto = await servicio.CrearAsync(Usuario, false, Solicitud(titulo: "Feria para tomar cerveza"), null);

        Assert.Equal(Evento.EstadoPendiente, dto.Estado);
        Assert.Contains(ReglasEvento.MensajeContenidoPendiente, dto.Motivo);
        Assert.Single(repo.Eventos);
    }

    [Fact]
    public async Task El_limite_cuenta_eventos_cancelados_y_se_aplica_al_dia_local()
    {
        var (servicio, repo) = CrearServicio();
        for (var i = 0; i < ReglasEvento.MaximoEventosPorDia; i++)
        {
            repo.Eventos.Add(new Evento
            {
                Id = i + 1,
                CreadorId = Usuario,
                Estado = Evento.EstadoCancelado,
                CreatedAt = Ahora.UtcDateTime
            });
        }

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync(Usuario, false, Solicitud(), null));

        Assert.Equal("LIMITE_DIARIO", error.Codigo);
        Assert.Equal(3, repo.Eventos.Count);
    }

    [Fact]
    public async Task Administrador_publica_directamente_y_no_cuenta_en_el_limite()
    {
        var (servicio, repo) = CrearServicio();
        for (var i = 0; i < ReglasEvento.MaximoEventosPorDia; i++)
            repo.Eventos.Add(new Evento { Id = i + 1, CreadorId = Usuario, CreatedAt = Ahora.UtcDateTime });
        var solicitud = Solicitud(
            titulo: "Evento especial",
            lugarId: null,
            lugarTexto: "Fuera del campus",
            inicio: new DateTimeOffset(2026, 11, 17, 23, 0, 0, TimeSpan.FromHours(-4)),
            fin: new DateTimeOffset(2026, 11, 17, 23, 30, 0, TimeSpan.FromHours(-4)));

        var dto = await servicio.CrearAsync(Usuario, true, solicitud, null);

        Assert.Equal(Evento.EstadoPublicado, dto.Estado);
        Assert.Equal(Evento.OrigenAdmin, dto.Origen);
        Assert.Equal(4, repo.Eventos.Count);
    }

    // HU34: el FE crea un evento por cada fecha del afiche con POST /api/eventos (origen FLYER). Cada uno se evalúa solo.
    [Fact]
    public async Task Cada_fecha_de_un_afiche_es_un_evento_aparte_que_puede_quedar_pendiente_por_separado()
    {
        var (servicio, repo) = CrearServicio();
        var hora = TimeSpan.FromHours(-4);

        var publicada = await servicio.CrearAsync(Usuario, false, Solicitud(origen: "FLYER",
            inicio: new DateTimeOffset(2026, 11, 18, 18, 0, 0, hora), fin: new DateTimeOffset(2026, 11, 18, 20, 0, 0, hora)), null);
        var deNoche = await servicio.CrearAsync(Usuario, false, Solicitud(origen: "FLYER",
            inicio: new DateTimeOffset(2026, 11, 20, 20, 0, 0, hora), fin: new DateTimeOffset(2026, 11, 20, 22, 0, 0, hora)), null);

        Assert.Equal(Evento.EstadoPublicado, publicada.Estado);
        Assert.Equal(Evento.EstadoPendiente, deNoche.Estado);   // fuera del horario: solo esa fecha va a revisión
        Assert.All(repo.Eventos, e => Assert.Equal(Evento.OrigenFlyer, e.Origen));
        Assert.Equal(2, repo.Eventos.Select(e => e.Inicio.Date).Distinct().Count());
    }

    [Fact]
    public async Task Las_fechas_de_un_afiche_cuentan_en_el_limite_de_3_por_dia_y_el_admin_no_tiene_limite()
    {
        var (servicio, repo) = CrearServicio();
        var hora = TimeSpan.FromHours(-4);
        EventoFormularioRequest Fecha(int dia) => Solicitud(origen: "FLYER",
            inicio: new DateTimeOffset(2026, 11, dia, 13, 0, 0, hora), fin: new DateTimeOffset(2026, 11, dia, 14, 0, 0, hora));

        for (var dia = 18; dia < 21; dia++) await servicio.CrearAsync(Usuario, false, Fecha(dia), null);
        var cuarta = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.CrearAsync(Usuario, false, Fecha(21), null));

        Assert.Equal("LIMITE_DIARIO", cuarta.Codigo);
        Assert.Equal(3, repo.Eventos.Count);

        for (var dia = 18; dia < 23; dia++) await servicio.CrearAsync(Admin, true, Fecha(dia), null);
        Assert.Equal(5, repo.Eventos.Count(e => e.CreadorId == Admin));
    }

    [Fact]
    public async Task Un_evento_creado_desde_un_flyer_se_guarda_con_origen_FLYER_y_pasa_las_mismas_reglas()
    {
        var (servicio, repo) = CrearServicio();

        var dto = await servicio.CrearAsync(Usuario, false, Solicitud(origen: "FLYER"), null);

        Assert.Equal(Evento.OrigenFlyer, dto.Origen);
        Assert.Equal(Evento.OrigenFlyer, Assert.Single(repo.Eventos).Origen);
        Assert.Equal(Evento.EstadoPublicado, dto.Estado);

        var (otro, repoOtro) = CrearServicio();
        var pendiente = await otro.CrearAsync(
            Usuario, false, Solicitud(lugarId: null, lugarTexto: "Patio lateral", origen: "FLYER"), null);
        Assert.Equal(Evento.EstadoPendiente, pendiente.Estado);
        Assert.Equal(Evento.OrigenFlyer, Assert.Single(repoOtro.Eventos).Origen);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("FORMULARIO")]
    public void Sin_origen_o_con_FORMULARIO_el_evento_se_guarda_como_FORMULARIO(string? origen)
    {
        var (servicio, repo) = CrearServicio();

        servicio.CrearAsync(Usuario, false, Solicitud(origen: origen), null).GetAwaiter().GetResult();

        Assert.Equal(Evento.OrigenFormulario, Assert.Single(repo.Eventos).Origen);
    }

    [Theory]
    [InlineData("ADMIN")]
    [InlineData("INSTAGRAM")]
    public async Task Un_origen_distinto_de_FORMULARIO_o_FLYER_se_rechaza_y_no_se_persiste(string origen)
    {
        var (servicio, repo) = CrearServicio();

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync(Usuario, false, Solicitud(origen: origen), null));

        Assert.Equal("VALIDACION", error.Codigo);
        Assert.Equal(ReglasEvento.MensajeOrigenInvalido, error.Message);
        Assert.Empty(repo.Eventos);
    }

    [Fact]
    public async Task El_administrador_conserva_el_origen_ADMIN_aunque_pida_FLYER()
    {
        var (servicio, _) = CrearServicio();

        var dto = await servicio.CrearAsync(Usuario, true, Solicitud(origen: "FLYER"), null);

        Assert.Equal(Evento.OrigenAdmin, dto.Origen);
    }

    [Fact]
    public async Task Editar_un_evento_no_cambia_su_origen()
    {
        var (servicio, repo) = CrearServicio();
        var creado = await servicio.CrearAsync(Usuario, false, Solicitud(origen: "FLYER"), null);

        await servicio.ActualizarAsync(creado.Id, Usuario, Solicitud(titulo: "Otro título", origen: "FORMULARIO"), null);

        Assert.Equal(Evento.OrigenFlyer, Assert.Single(repo.Eventos).Origen);
    }

    [Fact]
    public async Task Campos_obligatorios_invalidos_no_se_persisten()
    {
        var (servicio, repo) = CrearServicio();

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync(Usuario, false, Solicitud(titulo: " "), null));

        Assert.Equal("VALIDACION", error.Codigo);
        Assert.Empty(repo.Eventos);
    }

    [Fact]
    public async Task Usuario_solo_puede_cancelar_sus_eventos_y_no_luego_del_inicio()
    {
        var (servicio, repo) = CrearServicio();
        var creada = await servicio.CrearAsync(Usuario, false, Solicitud(), null);

        var ajeno = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.CancelarAsync(creada.Id, 99));
        var cancelada = await servicio.CancelarAsync(creada.Id, Usuario);

        Assert.Equal("NO_ENCONTRADO", ajeno.Codigo);
        Assert.Equal(Evento.EstadoCancelado, cancelada.Estado);

        var iniciada = Assert.Single(repo.Eventos);
        iniciada.Inicio = Ahora.UtcDateTime.AddMinutes(-1);
        var tarde = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.CancelarAsync(creada.Id, Usuario));
        Assert.Equal("EVENTO_INICIADO", tarde.Codigo);
    }

    [Theory]
    [InlineData(Evento.EstadoRechazado)]
    [InlineData(Evento.EstadoCancelado)]
    public async Task Un_evento_rechazado_o_cancelado_no_se_puede_editar(string estado)
    {
        var (servicio, repo) = CrearServicio();
        repo.Eventos.Add(new Evento
        {
            Id = 1,
            CreadorId = Usuario,
            Titulo = "Original",
            Estado = estado,
            LugarId = 3,
            CreatedAt = Ahora.UtcDateTime,
            UpdatedAt = Ahora.UtcDateTime,
            Inicio = Ahora.UtcDateTime.AddDays(1),
            Fin = Ahora.UtcDateTime.AddDays(1).AddHours(1)
        });

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(1, Usuario, Solicitud(titulo: "Cambiado"), null));

        Assert.Equal("EVENTO_NO_EDITABLE", error.Codigo);
        var evento = Assert.Single(repo.Eventos);
        Assert.Equal("Original", evento.Titulo);
        Assert.Equal(estado, evento.Estado);
    }

    private static readonly byte[] FotoJpg = [0xFF, 0xD8, 0xFF, 0x01];
    private static readonly byte[] FotoPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x02];

    private static async Task<(EventoService Servicio, RepositorioFalso Repo, int Id)> EventoConFotoAsync()
    {
        var (servicio, repo) = CrearServicio();
        var creado = await servicio.CrearAsync(Usuario, false, Solicitud(), FotoJpg);
        return (servicio, repo, creado.Id);
    }

    [Fact]
    public async Task Editar_con_una_foto_nueva_reemplaza_la_anterior()
    {
        var (servicio, repo, id) = await EventoConFotoAsync();
        repo.Imagenes[id] = (FotoJpg, "image/jpeg", Ahora.UtcDateTime.AddDays(-1)); // la foto se subió ayer
        var antes = (await servicio.ListarMiosAsync(Usuario)).Single().FotoUrl;

        var dto = await servicio.ActualizarAsync(id, Usuario, Solicitud(titulo: "Con foto nueva"), FotoPng);

        var imagen = Assert.Single(repo.Imagenes).Value;
        Assert.Equal(FotoPng, imagen.Contenido);
        Assert.Equal("image/png", imagen.TipoMime);
        Assert.NotNull(dto.FotoUrl);
        Assert.NotEqual(antes, dto.FotoUrl);
    }

    [Fact]
    public async Task Editar_sin_tocar_la_foto_la_conserva()
    {
        var (servicio, repo, id) = await EventoConFotoAsync();

        var dto = await servicio.ActualizarAsync(id, Usuario, Solicitud(titulo: "Solo cambia el título"), null);

        Assert.Equal(FotoJpg, Assert.Single(repo.Imagenes).Value.Contenido);
        Assert.NotNull(dto.FotoUrl);
    }

    [Fact]
    public async Task Quitar_la_foto_al_editar_la_borra_y_el_evento_queda_sin_fotoUrl()
    {
        var (servicio, repo, id) = await EventoConFotoAsync();
        var solicitud = Solicitud(titulo: "Sin foto") with { QuitarFoto = true };

        var dto = await servicio.ActualizarAsync(id, Usuario, solicitud, null);

        Assert.Empty(repo.Imagenes);
        Assert.Null(dto.FotoUrl);
        Assert.Equal("Sin foto", dto.Titulo);
        Assert.Null((await servicio.ListarMiosAsync(Usuario)).Single().FotoUrl);
    }

    [Fact]
    public async Task Quitar_la_foto_de_un_evento_sin_foto_no_falla()
    {
        var (servicio, repo) = CrearServicio();
        var creado = await servicio.CrearAsync(Usuario, false, Solicitud(), null);

        var dto = await servicio.ActualizarAsync(creado.Id, Usuario, Solicitud() with { QuitarFoto = true }, null);

        Assert.Empty(repo.Imagenes);
        Assert.Null(dto.FotoUrl);
    }

    [Fact]
    public async Task No_se_puede_subir_una_foto_nueva_y_quitar_la_actual_a_la_vez()
    {
        var (servicio, repo, id) = await EventoConFotoAsync();

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(id, Usuario, Solicitud(titulo: "Cambiado") with { QuitarFoto = true }, FotoPng));

        Assert.Equal("VALIDACION", error.Codigo);
        Assert.Equal(ReglasEvento.MensajeFotoNuevaYQuitar, error.Message);
        Assert.Equal(FotoJpg, Assert.Single(repo.Imagenes).Value.Contenido);
        Assert.Equal("Feria de ciencias", repo.Eventos.Single().Titulo);
    }

    [Fact]
    public async Task Solo_el_dueno_puede_cambiar_o_quitar_la_foto()
    {
        var (servicio, repo, id) = await EventoConFotoAsync();

        var alQuitar = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(id, 99, Solicitud() with { QuitarFoto = true }, null));
        var alCambiar = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(id, 99, Solicitud(), FotoPng));

        Assert.Equal("NO_ENCONTRADO", alQuitar.Codigo);
        Assert.Equal("NO_ENCONTRADO", alCambiar.Codigo);
        Assert.Equal(FotoJpg, Assert.Single(repo.Imagenes).Value.Contenido);
    }

    [Theory]
    [InlineData(Evento.EstadoRechazado)]
    [InlineData(Evento.EstadoCancelado)]
    public async Task En_un_evento_no_editable_no_se_toca_la_foto(string estado)
    {
        var (servicio, repo, id) = await EventoConFotoAsync();
        repo.Eventos.Single().Estado = estado;

        var alQuitar = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(id, Usuario, Solicitud() with { QuitarFoto = true }, null));
        var alCambiar = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(id, Usuario, Solicitud(), FotoPng));

        Assert.Equal("EVENTO_NO_EDITABLE", alQuitar.Codigo);
        Assert.Equal("EVENTO_NO_EDITABLE", alCambiar.Codigo);
        Assert.Equal(FotoJpg, Assert.Single(repo.Imagenes).Value.Contenido);
    }

    [Fact]
    public async Task Un_evento_que_ya_empezo_no_permite_cambiar_ni_quitar_la_foto()
    {
        var (servicio, repo, id) = await EventoConFotoAsync();
        repo.Eventos.Single().Inicio = Ahora.UtcDateTime.AddMinutes(-5);

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ActualizarAsync(id, Usuario, Solicitud() with { QuitarFoto = true }, null));

        Assert.Equal("EVENTO_INICIADO", error.Codigo);
        Assert.Single(repo.Imagenes);
    }

    [Fact]
    public async Task Una_foto_nueva_invalida_se_rechaza_y_la_anterior_se_conserva()
    {
        var (servicio, repo, id) = await EventoConFotoAsync();
        var noEsImagen = new byte[] { 1, 2, 3, 4 };
        var demasiadoGrande = new byte[ReglasEvento.BytesMaximosImagen + 1];
        demasiadoGrande[0] = 0xFF; demasiadoGrande[1] = 0xD8; demasiadoGrande[2] = 0xFF;

        foreach (var invalida in new[] { noEsImagen, demasiadoGrande, Array.Empty<byte>() })
        {
            var error = await Assert.ThrowsAsync<ReglaNegocioException>(
                () => servicio.ActualizarAsync(id, Usuario, Solicitud(titulo: "No debe cambiar"), invalida));
            Assert.Equal("VALIDACION", error.Codigo);
        }

        Assert.Equal(FotoJpg, Assert.Single(repo.Imagenes).Value.Contenido);
        Assert.Equal("Feria de ciencias", repo.Eventos.Single().Titulo);
    }

    [Fact]
    public async Task Cambiar_la_foto_no_altera_el_refiltro_del_texto_al_editar()
    {
        var (servicio, _, id) = await EventoConFotoAsync();

        var dto = await servicio.ActualizarAsync(
            id, Usuario, Solicitud(lugarId: null, lugarTexto: "Patio lateral"), FotoPng);

        Assert.Equal(Evento.EstadoPendiente, dto.Estado);
        Assert.NotNull(dto.FotoUrl);
    }

    [Theory]
    [InlineData(Evento.EstadoPendiente)]
    [InlineData(Evento.EstadoPublicado)]
    public async Task Un_evento_pendiente_o_publicado_se_puede_editar_y_queda_publicado(string estado)
    {
        var (servicio, repo) = CrearServicio();
        repo.Eventos.Add(new Evento
        {
            Id = 1,
            CreadorId = Usuario,
            Titulo = "Original",
            Estado = estado,
            LugarId = 3,
            CreatedAt = Ahora.UtcDateTime,
            UpdatedAt = Ahora.UtcDateTime,
            Inicio = Ahora.UtcDateTime.AddDays(1),
            Fin = Ahora.UtcDateTime.AddDays(1).AddHours(1)
        });

        var dto = await servicio.ActualizarAsync(1, Usuario, Solicitud(titulo: "Cambiado"), null);

        Assert.Equal("Cambiado", dto.Titulo);
        Assert.Equal(Evento.EstadoPublicado, dto.Estado);
    }

    [Fact]
    public async Task Los_pendientes_vencidos_no_se_listan_ni_se_pueden_aprobar()
    {
        var (servicio, repo) = CrearServicio();
        repo.Eventos.Add(new Evento
        {
            Id = 1,
            CreadorId = Usuario,
            Titulo = "Vencido",
            Estado = Evento.EstadoPendiente,
            LugarId = 3,
            CreatedAt = Ahora.UtcDateTime,
            UpdatedAt = Ahora.UtcDateTime,
            Inicio = Ahora.UtcDateTime.AddHours(-2),
            Fin = Ahora.UtcDateTime.AddHours(-1)
        });
        repo.Eventos.Add(new Evento
        {
            Id = 2,
            CreadorId = Usuario,
            Titulo = "Vigente",
            Estado = Evento.EstadoPendiente,
            LugarId = 3,
            CreatedAt = Ahora.UtcDateTime,
            UpdatedAt = Ahora.UtcDateTime,
            Inicio = Ahora.UtcDateTime.AddHours(1),
            Fin = Ahora.UtcDateTime.AddHours(2)
        });

        var pendientes = await servicio.ListarPendientesAsync();

        Assert.Equal("Vigente", Assert.Single(pendientes).Titulo);
        var error = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ModerarAsync(1, Admin, new ResolverEventoRequest(Evento.EstadoPublicado, null)));
        Assert.Equal("EVENTO_FINALIZADO", error.Codigo);
        Assert.Equal(Evento.EstadoPendiente, repo.Eventos.Single(e => e.Id == 1).Estado);
    }

    [Fact]
    public async Task La_foto_de_un_creador_suspendido_se_oculta_a_los_demas_pero_no_al_dueno_ni_al_admin()
    {
        var (servicio, repo) = CrearServicio();
        repo.Eventos.Add(new Evento
        {
            Id = 1,
            CreadorId = Usuario,
            Estado = Evento.EstadoPublicado,
            LugarId = 3,
            CreatedAt = Ahora.UtcDateTime,
            UpdatedAt = Ahora.UtcDateTime,
            Inicio = Ahora.UtcDateTime.AddDays(1),
            Fin = Ahora.UtcDateTime.AddDays(1).AddHours(1)
        });
        repo.Imagenes[1] = ([0xFF, 0xD8, 0xFF], "image/jpeg", Ahora.UtcDateTime);
        repo.Suspendidos.Add(Usuario);

        Assert.NotNull(await servicio.ObtenerImagenAsync(1, Usuario, false));
        Assert.NotNull(await servicio.ObtenerImagenAsync(1, 99, true));
        var error = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ObtenerImagenAsync(1, 99, false));
        Assert.Equal("NO_ENCONTRADO", error.Codigo);
    }

    [Fact]
    public async Task Moderar_notifica_a_traves_del_repositorio_y_no_revisa_dos_veces()
    {
        var (servicio, repo) = CrearServicio();
        var creada = await servicio.CrearAsync(Usuario, false, Solicitud(lugarId: null), null);

        await servicio.ModerarAsync(creada.Id, Admin, new ResolverEventoRequest(Evento.EstadoPublicado, null));
        var repetida = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ModerarAsync(creada.Id, Admin, new ResolverEventoRequest(Evento.EstadoRechazado, "No cumple")));

        Assert.Equal(1, repo.VecesModerado);
        Assert.Equal("YA_REVISADO", repetida.Codigo);
        Assert.Equal(Evento.EstadoPublicado, Assert.Single(repo.Eventos).Estado);
    }

    // ── Campanita del admin (migración 030) ──

    private static (EventoService Servicio, RepositorioFalso Repo, AvisosAdminTests.AvisosFalsos Avisos) ConAvisos()
    {
        var repo = new RepositorioFalso();
        var avisos = new AvisosAdminTests.AvisosFalsos();
        return (new EventoService(repo, new RelojFijo(Ahora), avisos), repo, avisos);
    }

    [Fact]
    public async Task Un_evento_que_queda_pendiente_avisa_a_los_admins()
    {
        var (servicio, _, avisos) = ConAvisos();
        await servicio.CrearAsync(Usuario, false, Solicitud(lugarId: null, lugarTexto: "Patio lateral"), null);
        Assert.Equal(["evento-por-revisar:1"], avisos.Llamadas);
    }

    [Fact]
    public async Task Un_evento_publicado_no_queda_como_pendiente_para_el_admin()
    {
        var (servicio, _, avisos) = ConAvisos();
        await servicio.CrearAsync(Usuario, false, Solicitud(), null);
        Assert.DoesNotContain(avisos.Llamadas, l => l.StartsWith("evento-por-revisar"));
    }

    [Fact]
    public async Task Moderar_o_cancelar_cierra_el_aviso_del_admin()
    {
        var (servicio, _, avisos) = ConAvisos();
        var creado = await servicio.CrearAsync(Usuario, false, Solicitud(lugarId: null, lugarTexto: "Patio lateral"), null);
        await servicio.ModerarAsync(creado.Id, Admin, new ResolverEventoRequest(Evento.EstadoPublicado, null));
        Assert.Equal(["evento-por-revisar:1", "evento-revisado:1"], avisos.Llamadas);

        var otro = await servicio.CrearAsync(Usuario, false, Solicitud(lugarId: null, lugarTexto: "Patio trasero"), null);
        await servicio.CancelarAsync(otro.Id, Usuario);
        Assert.Equal("evento-revisado:2", avisos.Llamadas[^1]);
    }

    private static Evento EventoVisible(
        int id, DateTime inicio, DateTime fin, int creador = Usuario, int? lugarId = 3,
        string? lugarTexto = null, string estado = Evento.EstadoPublicado)
        => new()
        {
            Id = id,
            CreadorId = creador,
            Titulo = $"Evento {id}",
            Estado = estado,
            LugarId = lugarId,
            LugarTexto = lugarTexto,
            Asistencia = ReglasEvento.AsistenciaFija,
            Inicio = inicio,
            Fin = fin,
            CreatedAt = Ahora.UtcDateTime,
            UpdatedAt = Ahora.UtcDateTime
        };

    [Fact]
    public async Task Hoy_y_esta_semana_solo_listan_publicados_que_no_terminaron_y_ordenados_por_inicio()
    {
        var (servicio, repo) = CrearServicio();
        var ahora = Ahora.UtcDateTime;
        repo.Eventos.Add(EventoVisible(1, ahora.AddHours(2), ahora.AddHours(3)));
        repo.Eventos.Add(EventoVisible(2, ahora.AddHours(-3), ahora.AddHours(-1)));
        repo.Eventos.Add(EventoVisible(3, ahora.AddHours(1), ahora.AddHours(2), estado: Evento.EstadoPendiente));
        repo.Eventos.Add(EventoVisible(4, ahora.AddDays(1), ahora.AddDays(1).AddHours(1)));
        repo.Eventos.Add(EventoVisible(5, ahora.AddHours(1), ahora.AddHours(2), creador: 99));
        repo.Eventos.Add(EventoVisible(6, ahora.AddDays(8), ahora.AddDays(8).AddHours(1)));
        repo.Suspendidos.Add(99);

        var hoy = await servicio.ListarPublicadosAsync("hoy");
        var semana = await servicio.ListarPublicadosAsync("Semana");

        Assert.Equal([1], hoy.Select(e => e.Id));
        Assert.Equal([1, 4], semana.Select(e => e.Id));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("mes")]
    public async Task Un_rango_distinto_de_hoy_y_semana_da_error_de_validacion(string? rango)
    {
        var (servicio, _) = CrearServicio();

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.ListarPublicadosAsync(rango));

        Assert.Equal("VALIDACION", error.Codigo);
    }

    [Fact]
    public async Task La_tarjeta_usa_el_lugar_del_catalogo_o_el_texto_marcado_como_fuera_del_campus()
    {
        var (servicio, repo) = CrearServicio();
        var ahora = Ahora.UtcDateTime;
        repo.Eventos.Add(EventoVisible(1, ahora.AddHours(1), ahora.AddHours(2)));
        repo.Eventos.Add(EventoVisible(2, ahora.AddHours(2), ahora.AddHours(3), lugarId: null, lugarTexto: "Plaza 14 de Septiembre"));

        var lista = await servicio.ListarPublicadosAsync("hoy");

        var catalogo = lista.Single(e => e.Id == 1);
        Assert.Equal("Biblioteca", catalogo.Lugar);
        Assert.False(catalogo.FueraDelCampus);
        var fuera = lista.Single(e => e.Id == 2);
        Assert.Equal("Plaza 14 de Septiembre", fuera.Lugar);
        Assert.True(fuera.FueraDelCampus);
    }

    [Fact]
    public async Task El_detalle_solo_existe_para_eventos_publicados()
    {
        var (servicio, repo) = CrearServicio();
        var ahora = Ahora.UtcDateTime;
        repo.Eventos.Add(EventoVisible(1, ahora.AddHours(1), ahora.AddHours(2)));
        repo.Eventos.Add(EventoVisible(2, ahora.AddHours(1), ahora.AddHours(2), estado: Evento.EstadoPendiente));

        Assert.Equal("Evento 1", (await servicio.ObtenerPublicadoAsync(1)).Titulo);
        foreach (var id in new[] { 2, 99 })
        {
            var error = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.ObtenerPublicadoAsync(id));
            Assert.Equal("NO_ENCONTRADO", error.Codigo);
        }
    }

    // ── HU26: moderación de eventos por el admin ──

    [Fact]
    public async Task El_admin_edita_y_aprueba_en_un_solo_paso()
    {
        var (servicio, repo) = CrearServicio();
        var ahora = Ahora.UtcDateTime;
        repo.Eventos.Add(EventoVisible(1, ahora.AddHours(1), ahora.AddHours(2), lugarId: null, lugarTexto: "Patio",
            estado: Evento.EstadoPendiente));

        await servicio.ModerarAsync(1, Admin, new ResolverEventoRequest(
            Evento.EstadoPublicado, null, Solicitud(titulo: "  Feria corregida  ", lugarId: 3)));

        var evento = Assert.Single(repo.Eventos);
        Assert.Equal(Evento.EstadoPublicado, evento.Estado);
        Assert.Equal("Feria corregida", evento.Titulo);
        Assert.Equal(3, evento.LugarId);
        Assert.Null(evento.LugarTexto);
        var m = Assert.Single(repo.Moderaciones);
        Assert.Equal((Admin, Notificacion.TipoEventoAprobado), (m.AdminId, m.TipoAviso));
    }

    [Fact]
    public async Task Editar_y_aprobar_valida_los_campos_como_el_formulario()
    {
        var (servicio, repo) = CrearServicio();
        var ahora = Ahora.UtcDateTime;
        repo.Eventos.Add(EventoVisible(1, ahora.AddHours(1), ahora.AddHours(2), estado: Evento.EstadoPendiente));

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.ModerarAsync(1, Admin,
            new ResolverEventoRequest(Evento.EstadoPublicado, null, Solicitud(titulo: "   "))));

        Assert.Equal("VALIDACION", error.Codigo);
        Assert.Equal(Evento.EstadoPendiente, repo.Eventos[0].Estado);
        Assert.Empty(repo.Moderaciones);
    }

    [Fact]
    public async Task No_se_puede_editar_al_rechazar()
    {
        var (servicio, repo) = CrearServicio();
        var ahora = Ahora.UtcDateTime;
        repo.Eventos.Add(EventoVisible(1, ahora.AddHours(1), ahora.AddHours(2), estado: Evento.EstadoPendiente));

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.ModerarAsync(1, Admin,
            new ResolverEventoRequest(Evento.EstadoRechazado, "Spam", Solicitud())));

        Assert.Equal(ReglasEvento.MensajeCambiosSoloAlAprobar, error.Message);
    }

    [Fact]
    public async Task Rechazar_exige_motivo_y_lo_manda_al_autor()
    {
        var (servicio, repo) = CrearServicio();
        var ahora = Ahora.UtcDateTime;
        repo.Eventos.Add(EventoVisible(1, ahora.AddHours(1), ahora.AddHours(2), estado: Evento.EstadoPendiente));

        var sinMotivo = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.ModerarAsync(1, Admin, new ResolverEventoRequest(Evento.EstadoRechazado, "  ")));
        Assert.Equal(ReglasEvento.MensajeFaltaMotivoRechazo, sinMotivo.Message);

        await servicio.ModerarAsync(1, Admin, new ResolverEventoRequest(Evento.EstadoRechazado, "Es publicidad"));

        Assert.Equal((Evento.EstadoRechazado, "Es publicidad"), (repo.Eventos[0].Estado, repo.Eventos[0].Motivo));
        var m = Assert.Single(repo.Moderaciones);
        Assert.Equal((Notificacion.TipoEventoRechazado, "Es publicidad"), (m.TipoAviso, m.Motivo));
    }

    [Fact]
    public async Task Bajar_un_publicado_lo_oculta_con_motivo_y_avisa_al_autor()
    {
        var (servicio, repo) = CrearServicio();
        var ahora = Ahora.UtcDateTime;
        repo.Eventos.Add(EventoVisible(1, ahora.AddHours(1), ahora.AddHours(2)));

        await servicio.BajarAsync(1, Admin, new BajarEventoRequest("Contenido ofensivo"));

        Assert.Equal((Evento.EstadoRechazado, "Contenido ofensivo"), (repo.Eventos[0].Estado, repo.Eventos[0].Motivo));
        Assert.Empty(await servicio.ListarPublicadosAsync("semana"));
        var m = Assert.Single(repo.Moderaciones);
        Assert.Equal((Admin, Evento.EstadoPublicado, Notificacion.TipoEventoBajado), (m.AdminId, m.EstadoEsperado, m.TipoAviso));
    }

    [Fact]
    public async Task Bajar_exige_motivo_y_solo_sirve_para_publicados()
    {
        var (servicio, repo) = CrearServicio();
        var ahora = Ahora.UtcDateTime;
        repo.Eventos.Add(EventoVisible(1, ahora.AddHours(1), ahora.AddHours(2)));
        repo.Eventos.Add(EventoVisible(2, ahora.AddHours(1), ahora.AddHours(2), estado: Evento.EstadoPendiente));

        var sinMotivo = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.BajarAsync(1, Admin, new BajarEventoRequest(null)));
        Assert.Equal(("VALIDACION", ReglasEvento.MensajeFaltaMotivoBaja), (sinMotivo.Codigo, sinMotivo.Message));

        var pendiente = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.BajarAsync(2, Admin, new BajarEventoRequest("Spam")));
        Assert.Equal(("YA_REVISADO", ReglasEvento.MensajeEventoNoPublicado), (pendiente.Codigo, pendiente.Message));

        var noExiste = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.BajarAsync(99, Admin, new BajarEventoRequest("Spam")));
        Assert.Equal("NO_ENCONTRADO", noExiste.Codigo);
    }

    [Fact]
    public async Task El_admin_ve_los_publicados_que_no_terminaron_para_bajarlos()
    {
        var (servicio, repo) = CrearServicio();
        var ahora = Ahora.UtcDateTime;
        repo.Eventos.Add(EventoVisible(1, ahora.AddDays(10), ahora.AddDays(10).AddHours(1)));
        repo.Eventos.Add(EventoVisible(2, ahora.AddHours(1), ahora.AddHours(2)));
        repo.Eventos.Add(EventoVisible(3, ahora.AddHours(-3), ahora.AddHours(-2)));
        repo.Eventos.Add(EventoVisible(4, ahora.AddHours(1), ahora.AddHours(2), estado: Evento.EstadoPendiente));

        var lista = await servicio.ListarPublicadosAdminAsync();

        Assert.Equal([2, 1], lista.Select(e => e.Id));
    }

    // ── HU38: bajar un evento desde un reporte ──

    private static (EventoService Servicio, RepositorioFalso Repo, AvisosAdminTests.AvisosFalsos Avisos) ConEventoYReporte(
        string estadoReporte = "PENDIENTE", int eventoDelReporte = 1)
    {
        var (servicio, repo, avisos) = ConAvisos();
        var ahora = Ahora.UtcDateTime;
        repo.Eventos.Add(EventoVisible(1, ahora.AddHours(1), ahora.AddHours(2)));
        repo.Reportes.Add((31, eventoDelReporte, estadoReporte));
        return (servicio, repo, avisos);
    }

    [Fact]
    public async Task Bajar_desde_un_reporte_baja_el_evento_resuelve_el_reporte_y_cierra_el_aviso_de_los_admins()
    {
        var (servicio, repo, avisos) = ConEventoYReporte();

        await servicio.BajarAsync(1, Admin, new BajarEventoRequest("Contenido ofensivo", 31));

        Assert.Equal((Evento.EstadoRechazado, "Contenido ofensivo"), (repo.Eventos[0].Estado, repo.Eventos[0].Motivo));
        var m = Assert.Single(repo.Moderaciones);
        Assert.Equal((Notificacion.TipoEventoBajado, 31), (m.TipoAviso, m.ReporteId));
        var resuelto = Assert.Single(repo.ReportesResueltos);
        Assert.Equal((31, "Bajamos el evento. Motivo: Contenido ofensivo"), resuelto);
        Assert.Contains("reporte-revisado:31", avisos.Llamadas);
    }

    [Fact]
    public async Task Bajar_sin_reporte_no_toca_ningun_reporte()
    {
        var (servicio, repo, avisos) = ConEventoYReporte();

        await servicio.BajarAsync(1, Admin, new BajarEventoRequest("Spam"));

        Assert.Empty(repo.ReportesResueltos);
        Assert.Null(Assert.Single(repo.Moderaciones).ReporteId);
        Assert.DoesNotContain(avisos.Llamadas, l => l.StartsWith("reporte-revisado"));
    }

    [Fact]
    public async Task Bajar_desde_un_reporte_exige_motivo_y_no_cambia_nada()
    {
        var (servicio, repo, _) = ConEventoYReporte();

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.BajarAsync(1, Admin, new BajarEventoRequest("  ", 31)));

        Assert.Equal(("VALIDACION", ReglasEvento.MensajeFaltaMotivoBaja), (error.Codigo, error.Message));
        Assert.Equal(Evento.EstadoPublicado, repo.Eventos[0].Estado);
        Assert.Empty(repo.ReportesResueltos);
    }

    [Theory]
    [InlineData("PENDIENTE", 2, "REPORTE_NO_CORRESPONDE")] // el reporte es de otro evento
    [InlineData("RESUELTO", 1, "YA_REVISADO")]
    [InlineData("DESCARTADO", 1, "YA_REVISADO")]
    public async Task Un_reporte_que_no_corresponde_o_ya_no_esta_pendiente_no_deja_bajar_el_evento(string estado, int eventoDelReporte, string codigo)
    {
        var (servicio, repo, avisos) = ConEventoYReporte(estado, eventoDelReporte);

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.BajarAsync(1, Admin, new BajarEventoRequest("Spam", 31)));

        Assert.Equal(codigo, error.Codigo);
        Assert.Equal(Evento.EstadoPublicado, repo.Eventos[0].Estado);
        Assert.Empty(repo.Moderaciones);
        Assert.Empty(repo.ReportesResueltos);
        Assert.DoesNotContain(avisos.Llamadas, l => l.StartsWith("reporte-revisado"));
    }

    [Fact]
    public async Task Un_reporte_que_no_existe_no_deja_bajar_el_evento()
    {
        var (servicio, repo, _) = ConEventoYReporte();

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.BajarAsync(1, Admin, new BajarEventoRequest("Spam", 999)));

        Assert.Equal((ReglasEvento.CodigoReporteNoCorresponde, ReglasEvento.MensajeReporteNoCorresponde), (error.Codigo, error.Message));
        Assert.Equal(Evento.EstadoPublicado, repo.Eventos[0].Estado);
    }

    [Fact]
    public async Task Bajar_un_evento_que_ya_no_esta_publicado_desde_un_reporte_da_conflicto_y_deja_el_reporte_pendiente()
    {
        var (servicio, repo, _) = ConEventoYReporte();
        repo.Eventos[0].Estado = Evento.EstadoRechazado;

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.BajarAsync(1, Admin, new BajarEventoRequest("Spam", 31)));

        Assert.Equal(("YA_REVISADO", ReglasEvento.MensajeEventoNoPublicado), (error.Codigo, error.Message));
        Assert.Empty(repo.ReportesResueltos);
    }

    [Fact]
    public void La_respuesta_del_reporte_lleva_el_motivo_y_cabe_en_500_caracteres()
    {
        Assert.Equal("Bajamos el evento. Motivo: Spam", ReglasEvento.RespuestaReporteBaja("  Spam "));
        var larga = ReglasEvento.RespuestaReporteBaja(new string('x', 500));
        Assert.Equal(500, larga.Length);
        Assert.EndsWith("…", larga);
    }

    [Fact]
    public async Task El_historial_de_rechazados_distingue_rechazados_y_bajados_del_mas_reciente_al_mas_antiguo()
    {
        var (servicio, repo) = CrearServicio();
        var ahora = Ahora.UtcDateTime;
        repo.Eventos.Add(EventoVisible(1, ahora.AddHours(1), ahora.AddHours(2), estado: Evento.EstadoPendiente));
        repo.Eventos.Add(EventoVisible(2, ahora.AddHours(1), ahora.AddHours(2)));
        var viejo = EventoVisible(3, ahora.AddDays(-5), ahora.AddDays(-5).AddHours(1), estado: Evento.EstadoRechazado);
        viejo.Motivo = "Rechazado antes de HU26";
        viejo.UpdatedAt = ahora.AddDays(-6);
        repo.Eventos.Add(viejo);

        await servicio.ModerarAsync(1, Admin, new ResolverEventoRequest(Evento.EstadoRechazado, "Spam"));
        await servicio.BajarAsync(2, Admin, new BajarEventoRequest("Ofensivo"));

        var historial = await servicio.ListarRechazadosAsync();

        Assert.Equal([1, 2, 3], historial.Select(h => h.Evento.Id).OrderBy(id => id));
        Assert.Equal(3, historial[^1].Evento.Id);
        var bajado = historial.Single(h => h.Evento.Id == 2);
        Assert.True(bajado.FueBajado);
        Assert.Equal(("Ofensivo", "Admin"), (bajado.Evento.Motivo, bajado.Admin));
        Assert.False(historial.Single(h => h.Evento.Id == 1).FueBajado);
        var antiguo = historial.Single(h => h.Evento.Id == 3);
        Assert.Null(antiguo.Admin);
        Assert.Equal(viejo.UpdatedAt, antiguo.RechazadoEn.UtcDateTime);
    }

    // ── HU33: eventos duplicados ──

    [Fact]
    public async Task Reenviar_un_evento_igual_responde_409_y_no_guarda_otro()
    {
        var (servicio, repo) = CrearServicio();
        var primero = await servicio.CrearAsync(Usuario, false, Solicitud(), null);

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync(Usuario, false, Solicitud(), null));

        Assert.Equal(ReglasEvento.CodigoEventoDuplicado, error.Codigo);
        Assert.Contains(primero.Titulo, error.Message);
        Assert.Contains("#1", error.Message);
        Assert.Single(repo.Eventos);
    }

    [Fact]
    public async Task Otra_persona_que_repite_un_evento_lo_deja_pendiente_con_el_motivo()
    {
        var (servicio, repo) = CrearServicio();
        await servicio.CrearAsync(Usuario, false, Solicitud(), null);

        var dto = await servicio.CrearAsync(99, false, Solicitud(), null);

        Assert.Equal(Evento.EstadoPendiente, dto.Estado);
        Assert.Contains("Posible duplicado de «Feria de ciencias»", dto.Motivo);
        Assert.Contains("17/11/2026", dto.Motivo);
        Assert.Equal(2, repo.Eventos.Count);
    }

    [Fact]
    public async Task El_admin_publica_el_duplicado_y_recibe_un_aviso_en_la_respuesta()
    {
        var (servicio, repo) = CrearServicio();
        await servicio.CrearAsync(Usuario, false, Solicitud(), null);

        var dto = await servicio.CrearAsync(Admin, true, Solicitud(), null);

        Assert.Equal(Evento.EstadoPublicado, dto.Estado);
        Assert.NotNull(dto.Aviso);
        Assert.Contains("Feria de ciencias", dto.Aviso);
        Assert.Contains("#1", dto.Aviso);
        Assert.Equal(2, repo.Eventos.Count);
    }

    [Theory]
    [InlineData(Evento.EstadoRechazado)]
    [InlineData(Evento.EstadoCancelado)]
    public async Task Un_evento_rechazado_o_cancelado_no_bloquea_al_siguiente(string estado)
    {
        var (servicio, repo) = CrearServicio();
        await servicio.CrearAsync(Usuario, false, Solicitud(), null);
        repo.Eventos.Single().Estado = estado;

        var nuevo = await servicio.CrearAsync(Usuario, false, Solicitud(), null);

        Assert.Equal(Evento.EstadoPublicado, nuevo.Estado);
        Assert.Equal(2, repo.Eventos.Count);
    }

    [Fact]
    public async Task Si_hay_un_duplicado_ajeno_y_otro_propio_responde_409_aunque_el_ajeno_este_primero()
    {
        var (servicio, repo) = CrearServicio();
        await servicio.CrearAsync(99, false, Solicitud(), null);                       // ajeno, primero en la lista
        var propio = await servicio.CrearAsync(Usuario, false, Solicitud(), null);     // propio: queda PENDIENTE por el ajeno
        Assert.Equal(Evento.EstadoPendiente, propio.Estado);
        Assert.Equal(99, repo.Eventos[0].CreadorId);

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync(Usuario, false, Solicitud(), null));

        Assert.Equal(ReglasEvento.CodigoEventoDuplicado, error.Codigo);
        Assert.Contains($"#{propio.Id}", error.Message);
        Assert.Equal(2, repo.Eventos.Count);
    }

    [Fact]
    public async Task Si_solo_hay_un_duplicado_ajeno_queda_pendiente_con_el_motivo()
    {
        var (servicio, repo) = CrearServicio();
        await servicio.CrearAsync(99, false, Solicitud(), null);

        var dto = await servicio.CrearAsync(Usuario, false, Solicitud(), null);

        Assert.Equal(Evento.EstadoPendiente, dto.Estado);
        Assert.Contains("Posible duplicado de «Feria de ciencias»", dto.Motivo);
        Assert.Equal(2, repo.Eventos.Count);
    }

    [Fact]
    public async Task Si_solo_hay_un_duplicado_propio_responde_409()
    {
        var (servicio, repo) = CrearServicio();
        await servicio.CrearAsync(Usuario, false, Solicitud(), null);

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CrearAsync(Usuario, false, Solicitud(), null));

        Assert.Equal(ReglasEvento.CodigoEventoDuplicado, error.Codigo);
        Assert.Single(repo.Eventos);
    }

    [Fact]
    public async Task Si_el_propio_esta_cancelado_decide_el_ajeno_y_queda_pendiente()
    {
        var (servicio, repo) = CrearServicio();
        await servicio.CrearAsync(99, false, Solicitud(), null);
        await servicio.CrearAsync(Usuario, false, Solicitud(), null);
        repo.Eventos.Single(e => e.CreadorId == Usuario).Estado = Evento.EstadoCancelado;

        var dto = await servicio.CrearAsync(Usuario, false, Solicitud(), null);

        Assert.Equal(Evento.EstadoPendiente, dto.Estado);
        Assert.Contains("Posible duplicado de «Feria de ciencias»", dto.Motivo);
        Assert.Equal(3, repo.Eventos.Count);
    }

    [Fact]
    public async Task Con_varios_duplicados_ajenos_el_motivo_nombra_el_de_inicio_mas_cercano()
    {
        const string baseTitulo = "Feria de ciencias tecnologia arte musica cultura";
        var (servicio, repo) = CrearServicio();
        await servicio.CrearAsync(90, false, Solicitud(titulo: baseTitulo + " uno"), null);
        await servicio.CrearAsync(91, false, Solicitud(
            titulo: baseTitulo + " dos",
            inicio: new DateTimeOffset(2026, 11, 17, 14, 30, 0, TimeSpan.FromHours(-4)),
            fin: new DateTimeOffset(2026, 11, 17, 15, 30, 0, TimeSpan.FromHours(-4))), null);

        var dto = await servicio.CrearAsync(99, false, Solicitud(
            titulo: baseTitulo,
            inicio: new DateTimeOffset(2026, 11, 17, 14, 0, 0, TimeSpan.FromHours(-4)),
            fin: new DateTimeOffset(2026, 11, 17, 15, 0, 0, TimeSpan.FromHours(-4))), null);

        Assert.Equal(Evento.EstadoPendiente, dto.Estado);
        Assert.Contains("«" + baseTitulo + " dos»", dto.Motivo);
        Assert.DoesNotContain("uno»", dto.Motivo);
        Assert.Equal(3, repo.Eventos.Count);
    }

    [Fact]
    public async Task Editar_un_evento_no_lo_compara_consigo_mismo()
    {
        var (servicio, repo) = CrearServicio();
        var creado = await servicio.CrearAsync(Usuario, false, Solicitud(), null);

        var editado = await servicio.ActualizarAsync(creado.Id, Usuario, Solicitud(titulo: "Feria corregida"), null);

        Assert.Equal("Feria corregida", editado.Titulo);
        Assert.Single(repo.Eventos);
    }

    [Fact]
    public async Task La_tolerancia_de_tres_horas_solo_alcanza_a_las_fechas_cercanas()
    {
        var (servicio, repo) = CrearServicio();
        await servicio.CrearAsync(Usuario, false, Solicitud(), null);
        var cercano = await servicio.CrearAsync(99, false, Solicitud(
            inicio: new DateTimeOffset(2026, 11, 17, 15, 0, 0, TimeSpan.FromHours(-4)),
            fin: new DateTimeOffset(2026, 11, 17, 16, 0, 0, TimeSpan.FromHours(-4))), null);
        Assert.Equal(Evento.EstadoPendiente, cercano.Estado);

        var (otroServicio, otroRepo) = CrearServicio();
        await otroServicio.CrearAsync(Usuario, false, Solicitud(), null);
        var lejano = await otroServicio.CrearAsync(99, false, Solicitud(
            inicio: new DateTimeOffset(2026, 11, 17, 17, 0, 0, TimeSpan.FromHours(-4)),
            fin: new DateTimeOffset(2026, 11, 17, 18, 0, 0, TimeSpan.FromHours(-4))), null);

        Assert.Equal(Evento.EstadoPublicado, lejano.Estado);
        Assert.Equal(2, repo.Eventos.Count);
        Assert.Equal(2, otroRepo.Eventos.Count);
    }

    [Fact]
    public async Task Titulo_distinto_o_lugar_distinto_no_son_duplicados()
    {
        var (servicio, repo) = CrearServicio();
        await servicio.CrearAsync(Usuario, false, Solicitud(), null);

        await servicio.CrearAsync(99, false, Solicitud(titulo: "Concierto de guitarra"), null);
        await servicio.CrearAsync(99, false, Solicitud(lugarId: null, lugarTexto: "Patio lateral"), null);

        Assert.Equal(3, repo.Eventos.Count);
        Assert.All(repo.Eventos, e => Assert.DoesNotContain("Posible duplicado", e.Motivo ?? ""));
    }
}

public sealed class AvisoModeracionTests
{
    [Fact]
    public void El_aviso_de_rechazo_y_de_baja_llevan_el_motivo()
    {
        Assert.Equal("Tu evento «Feria» fue rechazado. Motivo: Es publicidad",
            ReglasEvento.AvisoModeracion(Notificacion.TipoEventoRechazado, "Feria", "Es publicidad"));
        Assert.Equal("Tu evento «Feria» fue retirado del apartado de eventos. Motivo: Spam",
            ReglasEvento.AvisoModeracion(Notificacion.TipoEventoBajado, "Feria", "Spam"));
        Assert.Equal("Tu evento «Feria» fue aprobado y ya está publicado.",
            ReglasEvento.AvisoModeracion(Notificacion.TipoEventoAprobado, "Feria", null));
    }

    [Theory]
    [InlineData(10, 500)]
    [InlineData(120, 500)]
    [InlineData(120, 5)]
    [InlineData(190, 0)]
    public void El_aviso_siempre_cabe_en_200_caracteres(int largoTitulo, int largoMotivo)
    {
        foreach (var tipo in new[] { Notificacion.TipoEventoRechazado, Notificacion.TipoEventoBajado, Notificacion.TipoEventoAprobado })
        {
            var aviso = ReglasEvento.AvisoModeracion(tipo, new string('t', largoTitulo),
                largoMotivo == 0 ? null : new string('m', largoMotivo));
            Assert.True(aviso.Length <= ReglasEvento.MaximoAviso, $"{tipo}: {aviso.Length}");
        }
    }

    [Fact]
    public void Un_motivo_largo_se_recorta_con_puntos_suspensivos()
    {
        var aviso = ReglasEvento.AvisoModeracion(Notificacion.TipoEventoRechazado, "Feria", new string('m', 500));

        Assert.Equal(ReglasEvento.MaximoAviso, aviso.Length);
        Assert.EndsWith("…", aviso);
        Assert.StartsWith("Tu evento «Feria» fue rechazado. Motivo: mmm", aviso);
    }
}
