using Breaku.Application.Comunidades;
using Breaku.Application.Horarios;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Tests;

public class ReglasDescubrirTests
{
    [Theory]
    [InlineData("Ana Pérez", "Ana P.")]
    [InlineData("ana maría quiroga", "ana M.")]
    [InlineData("Diego", "Diego")]
    [InlineData("  ", "Un compañero")]
    [InlineData(null, "Un compañero")]
    public void De_quien_la_creo_solo_se_muestra_el_nombre_y_la_inicial(string? nombre, string esperado)
        => Assert.Equal(esperado, ReglasDescubrir.NombreCorto(nombre));

    [Fact]
    public void Quien_la_crea_siempre_cuenta_aunque_no_tenga_su_fila()
    {
        Assert.Equal(3, ReglasDescubrir.Confirmados(3, creadorEnTabla: true));
        Assert.Equal(1, ReglasDescubrir.Confirmados(0, creadorEnTabla: false));
    }

    [Fact]
    public void Sin_campus_se_pide_elegirlo_antes_que_el_consentimiento()
        => Assert.Equal(ReglasDescubrir.EstadoSinZona, ReglasDescubrir.Estado(tieneZona: false, visible: false, 0).Estado);

    [Fact]
    public void Sin_consentimiento_se_avisa_aunque_haya_comunidades_propias()
    {
        var (estado, mensaje) = ReglasDescubrir.Estado(tieneZona: true, visible: false, 2);

        Assert.Equal(ReglasDescubrir.EstadoNoVisible, estado);
        Assert.Equal(ReglasDescubrir.MensajeNoVisible, mensaje);
    }

    [Fact]
    public void Con_campus_y_consentimiento_vacio_es_sin_resultados_y_con_datos_ok()
    {
        Assert.Equal((ReglasDescubrir.EstadoSinResultados, ReglasDescubrir.MensajeSinResultados), ReglasDescubrir.Estado(true, true, 0));
        Assert.Equal((ReglasDescubrir.EstadoOk, (string?)null), ReglasDescubrir.Estado(true, true, 1));
    }
}

public class DescubrirComunidadesServiceTests
{
    private static readonly DateTimeOffset AhoraUtc = new(2026, 11, 3, 14, 0, 0, TimeSpan.Zero);

    private sealed class RelojFijo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => AhoraUtc;
    }

    private sealed class RepositorioFalso(Usuario? usuario, List<ComunidadActiva> filas) : IDescubrirComunidadesRepository
    {
        public int? ZonaPedida { get; private set; }
        public DateTime HastaPedido { get; private set; }
        public bool Visible { get; set; }

        public Task<bool> PuedeVerAsync(int usuarioId, int comunidadId, int? deOtrosEnZona, DateTime ahoraUtc, CancellationToken ct = default)
        {
            ZonaPedida = deOtrosEnZona;
            return Task.FromResult(Visible);
        }

        public Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default) => Task.FromResult(usuario);

        public Task<List<ComunidadActiva>> ListarActivasAsync(int usuarioId, int? deOtrosEnZona, DateTime ahoraUtc, DateTime hastaUtc,
            int maximo, CancellationToken ct = default)
        {
            ZonaPedida = deOtrosEnZona;
            HastaPedido = hastaUtc;
            return Task.FromResult(filas);
        }
    }

    private static Usuario Estudiante(bool conZona = true, bool visible = true) => new()
    {
        Id = 7, Nombre = "Estudiante Dev", Activo = true, VisibleMatching = visible,
        ZonaActualId = conZona ? 2 : null,
        ZonaActual = conZona ? new Zona { Id = 2, Nombre = "Campus UCB Cochabamba", Activa = true } : null
    };

    private static ComunidadActiva Fila(int id, int creador, string nombre = "Ana Pérez", string? miEstado = null, int confirmados = 1) =>
        new(id, creador, nombre, "jugar cartas", new DateTime(2026, 11, 3, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 3, 21, 0, 0, DateTimeKind.Utc), 4, confirmados, true, miEstado);

    private static Task<ResultadoDescubrirDto> Listar(Usuario? usuario, params ComunidadActiva[] filas)
        => new DescubrirComunidadesService(new RepositorioFalso(usuario, [.. filas]), new RelojFijo()).ListarAsync(7);

    [Fact]
    public async Task Muestra_la_actividad_recien_creada_con_las_horas_de_Bolivia()
    {
        var r = await Listar(Estudiante(), Fila(12, creador: 7, nombre: "Estudiante Dev"));

        Assert.Equal(ReglasDescubrir.EstadoOk, r.Estado);
        var c = Assert.Single(r.Comunidades);
        Assert.Equal(new ComunidadDescubrirDto(12, "jugar cartas", "16:00", "17:00", 4, 1, "Tú", true, null), c);
    }

    [Fact]
    public async Task De_las_de_otros_muestra_el_nombre_corto_y_si_me_invitaron()
    {
        var r = await Listar(Estudiante(), Fila(13, creador: 9, miEstado: Participacion.EstadoPendiente, confirmados: 2));

        var c = Assert.Single(r.Comunidades);
        Assert.Equal("Ana P.", c.Creador);
        Assert.False(c.EsMia);
        Assert.Equal(Participacion.EstadoPendiente, c.MiEstado);
        Assert.Equal(2, c.Confirmados);
    }

    [Fact]
    public async Task Con_campus_y_consentimiento_pide_tambien_las_de_otros_de_su_sede()
    {
        var repo = new RepositorioFalso(Estudiante(), []);
        var r = await new DescubrirComunidadesService(repo, new RelojFijo()).ListarAsync(7);

        Assert.Equal(2, repo.ZonaPedida);
        Assert.Equal(ReglasDescubrir.EstadoSinResultados, r.Estado);
    }

    [Fact]
    public async Task Sin_consentimiento_no_pide_las_de_otros()
    {
        var repo = new RepositorioFalso(Estudiante(visible: false), []);
        var r = await new DescubrirComunidadesService(repo, new RelojFijo()).ListarAsync(7);

        Assert.Null(repo.ZonaPedida);
        Assert.Equal(ReglasDescubrir.EstadoNoVisible, r.Estado);
    }

    [Fact]
    public async Task Sin_campus_no_pide_las_de_otros()
    {
        var repo = new RepositorioFalso(Estudiante(conZona: false), []);
        var r = await new DescubrirComunidadesService(repo, new RelojFijo()).ListarAsync(7);

        Assert.Null(repo.ZonaPedida);
        Assert.Equal(ReglasDescubrir.EstadoSinZona, r.Estado);
    }

    [Fact]
    public async Task Solo_pide_las_de_hoy_hasta_la_medianoche_de_Bolivia()
    {
        var repo = new RepositorioFalso(Estudiante(), []);
        await new DescubrirComunidadesService(repo, new RelojFijo()).ListarAsync(7);

        Assert.Equal(new DateTime(2026, 11, 4, 4, 0, 0, DateTimeKind.Utc), repo.HastaPedido);
    }

    [Fact]
    public async Task Sin_cuenta_responde_no_encontrado()
    {
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => Listar(null));

        Assert.Equal("NO_ENCONTRADO", ex.Codigo);
    }

    [Fact]
    public async Task Solo_puede_suscribirse_a_una_comunidad_que_puede_ver()
    {
        var repo = new RepositorioFalso(Estudiante(), []) { Visible = true };
        var servicio = new DescubrirComunidadesService(repo, new RelojFijo());

        Assert.True(await servicio.PuedeVerAsync(7, 12));
        Assert.Equal(2, repo.ZonaPedida);

        repo.Visible = false;
        Assert.False(await servicio.PuedeVerAsync(7, 12));
    }

    [Fact]
    public async Task Sin_cuenta_no_puede_suscribirse()
        => Assert.False(await new DescubrirComunidadesService(new RepositorioFalso(null, []) { Visible = true }, new RelojFijo()).PuedeVerAsync(7, 12));
}
