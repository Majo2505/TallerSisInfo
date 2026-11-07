using Breaku.Application.Actividades;
using Breaku.Application.Horarios;
using Breaku.Application.Recomendaciones;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Tests;

/// <summary>HU10: el resumen que se confirma es el que se crea.</summary>
public class ReglasResumenActividadTests
{
    private static readonly DatosActividad Cartas = new("jugar cartas", new TimeOnly(16, 0), 60, 4);

    [Fact]
    public void El_resumen_se_guarda_y_se_lee_igual()
    {
        var json = ReglasResumenActividad.Serializar(Cartas);

        Assert.Equal("""{"actividad":"jugar cartas","horaInicio":"16:00","duracionMin":60,"maxParticipantes":4}""", json);
        Assert.Equal(Cartas, ReglasResumenActividad.Leer(json));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no es json")]
    public void Sin_resumen_guardado_se_lee_null(string? json) => Assert.Null(ReglasResumenActividad.Leer(json));

    [Fact]
    public void La_actividad_se_compara_sin_mayusculas_ni_espacios()
        => Assert.True(ReglasResumenActividad.Coinciden(Cartas, Cartas with { Actividad = "  Jugar Cartas " }));

    [Theory]
    [InlineData("jugar fútbol", 16, 60, 4)]
    [InlineData("jugar cartas", 17, 60, 4)]
    [InlineData("jugar cartas", 16, 90, 4)]
    [InlineData("jugar cartas", 16, 60, 30)]
    public void Cualquier_dato_distinto_no_coincide(string actividad, int hora, int duracion, int personas)
        => Assert.False(ReglasResumenActividad.Coinciden(Cartas, new DatosActividad(actividad, new TimeOnly(hora, 0), duracion, personas)));

    [Theory]
    [InlineData(2026, 11, 3, 14, 0, true)]    // 10:00 de hoy en Bolivia
    [InlineData(2026, 11, 4, 3, 30, true)]    // 23:30 de hoy en Bolivia (en UTC ya es mañana)
    [InlineData(2026, 11, 3, 3, 30, false)]   // 23:30 de ayer en Bolivia
    public void El_resumen_tiene_que_ser_de_hoy_en_hora_de_Bolivia(int a, int m, int d, int h, int min, bool esperado)
    {
        var ahora = new DateTime(2026, 11, 4, 3, 45, 0, DateTimeKind.Utc);   // 23:45 del 3 de noviembre en Bolivia
        Assert.Equal(esperado, ReglasResumenActividad.EsDeHoy(new DateTime(a, m, d, h, min, 0, DateTimeKind.Utc), ahora));
    }
}

/// <summary>HU10 y HU11: la extracción guarda el resumen y la creación lo exige.</summary>
public class ResumenConfirmadoServiciosTests
{
    // 15:00 en Bolivia (19:00 UTC).
    private static readonly DateTimeOffset Ahora = new(2026, 11, 3, 19, 0, 0, TimeSpan.Zero);
    private const int Usuario = 7;
    private const long Consulta = 58;

    private sealed class RelojFijo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Ahora;
    }

    private sealed class ActividadesFalsas : IActividadRepository
    {
        public LogAuditoriaRag Extraccion { get; } = new()
        {
            Id = Consulta, UsuarioId = Usuario, Tipo = LogAuditoriaRag.TipoExtraccionComunidad, Estado = LogAuditoriaRag.EstadoOk,
            CreatedAt = Ahora.UtcDateTime.AddMinutes(-2),
            DatosExtraidos = """{"actividad":"jugar cartas","horaInicio":"16:00","duracionMin":60,"maxParticipantes":4}"""
        };
        public List<ComunidadEfimera> Creadas { get; } = [];

        public Task<LogAuditoriaRag?> ObtenerExtraccionAsync(long consultaId, int usuarioId, CancellationToken ct = default)
            => Task.FromResult(consultaId == Extraccion.Id && usuarioId == Extraccion.UsuarioId ? Extraccion : null);
        public Task<bool> ConsultaYaUsadaAsync(long consultaId, CancellationToken ct = default) => Task.FromResult(false);
        public Task AgregarAsync(ComunidadEfimera comunidad, CancellationToken ct = default)
        {
            Creadas.Add(comunidad);
            return Task.CompletedTask;
        }
    }

    private static CrearActividadRequest Pedido(string actividad = "jugar cartas", string hora = "16:00", int duracion = 60, int personas = 4)
        => new("quiero jugar cartas a las 4 por una hora", actividad, hora, duracion, personas, Consulta);

    private static (CreacionActividadService Servicio, ActividadesFalsas Repo) Crear()
    {
        var repo = new ActividadesFalsas();
        return (new CreacionActividadService(repo, new RelojFijo()), repo);
    }

    [Fact]
    public async Task Se_crea_el_resumen_que_se_confirmo()
    {
        var (servicio, repo) = Crear();

        var creada = await servicio.CrearAsync(Usuario, Pedido(actividad: "Jugar Cartas"));

        var c = Assert.Single(repo.Creadas);
        Assert.Equal(("jugar cartas", (short)4), (c.Actividad, c.MaxParticipantes));
        Assert.Equal(("16:00", "17:00"), (creada.HoraInicio, creada.HoraFin));
    }

    [Theory]
    [InlineData("jugar fútbol", "16:00", 60, 4)]
    [InlineData("jugar cartas", "18:00", 60, 4)]
    [InlineData("jugar cartas", "16:00", 240, 4)]
    [InlineData("jugar cartas", "16:00", 60, 30)]
    public async Task Si_los_datos_no_son_los_del_resumen_no_se_crea(string actividad, string hora, int duracion, int personas)
    {
        var (servicio, repo) = Crear();

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.CrearAsync(Usuario, Pedido(actividad, hora, duracion, personas)));

        Assert.Equal("RESUMEN_DISTINTO", ex.Codigo);
        Assert.Empty(repo.Creadas);
    }

    [Fact]
    public async Task Un_resumen_de_otro_dia_no_sirve()
    {
        var (servicio, repo) = Crear();
        repo.Extraccion.CreatedAt = Ahora.UtcDateTime.AddDays(-1);

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.CrearAsync(Usuario, Pedido()));

        Assert.Equal(("VALIDACION", ReglasResumenActividad.MensajeOtroDia), (ex.Codigo, ex.Message));
        Assert.Empty(repo.Creadas);
    }

    [Fact]
    public async Task Una_extraccion_sin_resumen_guardado_se_sigue_aceptando()
    {
        var (servicio, repo) = Crear();
        repo.Extraccion.DatosExtraidos = null;   // hecha antes de la migración 013

        await servicio.CrearAsync(Usuario, Pedido(actividad: "tomar un café", hora: "17:00"));

        Assert.Equal("tomar un café", Assert.Single(repo.Creadas).Actividad);
    }

    [Fact]
    public async Task Una_consulta_ajena_responde_no_encontrado()
    {
        var (servicio, _) = Crear();
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.CrearAsync(99, Pedido()));
        Assert.Equal("NO_ENCONTRADO", ex.Codigo);
    }

    // ---------- HU11: la extracción guarda el resumen ----------

    private sealed class ExtraccionesFalsas : IExtraccionRepository
    {
        public List<LogAuditoriaRag> Logs { get; } = [];

        public Task GuardarLogAsync(LogAuditoriaRag log, CancellationToken ct = default)
        {
            log.Id = Logs.Count + 1;
            Logs.Add(log);
            return Task.CompletedTask;
        }
        public Task<LogAuditoriaRag?> ObtenerLogAsync(long id, CancellationToken ct = default)
            => Task.FromResult(Logs.FirstOrDefault(l => l.Id == id));
    }

    private sealed class ExtractorFalso(ArgumentosExtraidos argumentos) : IExtractorActividad
    {
        public Task<RespuestaExtractor> ExtraerAsync(PromptRag prompt, string texto, CancellationToken ct = default)
            => Task.FromResult(new RespuestaExtractor("falso", "{}", argumentos));
    }

    [Fact]
    public async Task Una_extraccion_completa_guarda_el_resumen_que_se_muestra()
    {
        var repo = new ExtraccionesFalsas();
        var servicio = new ExtraccionActividadService(
            new ExtractorFalso(new ArgumentosExtraidos(true, "jugar cartas", "16:00", null, 60, 4)), repo, new RelojFijo());

        var r = await servicio.ExtraerAsync(Usuario, new SolicitudExtraccionDto("quiero jugar cartas a las 4 por una hora", null));

        var guardado = ReglasResumenActividad.Leer(Assert.Single(repo.Logs).DatosExtraidos);
        Assert.Equal(new DatosActividad(r.Actividad.Actividad, ReglasExtraccion.LeerHora(r.Actividad.HoraInicio), r.Actividad.DuracionMin, r.Actividad.MaxParticipantes), guardado);
    }

    [Fact]
    public async Task Una_extraccion_incompleta_no_guarda_resumen()
    {
        var repo = new ExtraccionesFalsas();
        var servicio = new ExtraccionActividadService(
            new ExtractorFalso(new ArgumentosExtraidos(true, "jugar cartas", "16:00", null, null, 4)), repo, new RelojFijo());

        var r = await servicio.ExtraerAsync(Usuario, new SolicitudExtraccionDto("quiero jugar cartas a las 4", null));

        Assert.Equal(ReglasExtraccion.EstadoIncompleto, r.Estado);
        Assert.Null(Assert.Single(repo.Logs).DatosExtraidos);
    }

    [Fact]
    public async Task La_correccion_a_mano_tambien_guarda_el_resumen()
    {
        var repo = new ExtraccionesFalsas();
        var servicio = new ExtraccionActividadService(
            new ExtractorFalso(new ArgumentosExtraidos(false, null, null, null, null, null)), repo, new RelojFijo());

        await servicio.RevisarAsync(Usuario, new RevisionActividadDto("quiero jugar cartas",
            new ActividadExtraidaDto("jugar cartas", "17:00", 90, 6)));

        Assert.Equal(new DatosActividad("jugar cartas", new TimeOnly(17, 0), 90, 6),
            ReglasResumenActividad.Leer(Assert.Single(repo.Logs).DatosExtraidos));
    }
}
