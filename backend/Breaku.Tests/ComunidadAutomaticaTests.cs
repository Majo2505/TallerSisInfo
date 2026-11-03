using Breaku.Application.Actividades;
using Breaku.Application.Comunidades;
using Breaku.Application.Horarios;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Tests;

public class ReglasComunidadTests
{
    private static ReglasMatching.Franja F(int dia, int h1, int m1, int h2, int m2) => new(dia, new TimeOnly(h1, m1), new TimeOnly(h2, m2));
    private static ReglasMatching.PuenteDe P(int usuario, ReglasMatching.Franja f) => new(usuario, f);

    [Fact]
    public void Invita_primero_a_quien_comparte_mas_tiempo()
    {
        var actividad = F(2, 10, 30, 11, 30);
        var invitados = ReglasComunidad.ElegirInvitados(actividad, 3,
        [
            P(1, F(2, 11, 0, 12, 0)),
            P(2, F(2, 9, 30, 11, 30)),
            P(3, F(2, 10, 0, 11, 15)),
        ]);

        Assert.Equal([2, 3, 1], invitados.Select(i => i.UsuarioId));
        Assert.Equal(new TimeOnly(10, 30), invitados[0].EnComun.Inicio);
        Assert.Equal(new TimeOnly(11, 30), invitados[0].EnComun.Fin);
        Assert.Equal(60, invitados[0].MinutosEnComun);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(29, 6)]
    public void Invita_como_mucho_a_tantos_como_lugares_libres(int plazas, int esperados)
    {
        var actividad = F(2, 10, 0, 11, 0);
        var otros = Enumerable.Range(1, 6).Select(i => P(i, F(2, 10, 0, 11, 0)));
        Assert.Equal(esperados, ReglasComunidad.ElegirInvitados(actividad, plazas, otros).Count);
    }

    [Fact]
    public void Con_menos_de_30_minutos_en_comun_no_se_invita()
    {
        var actividad = F(2, 10, 0, 11, 0);
        Assert.Empty(ReglasComunidad.ElegirInvitados(actividad, 3, [P(1, F(2, 10, 40, 12, 0))]));
    }

    [Fact]
    public void Una_actividad_corta_basta_con_compartirla_entera()
    {
        var actividad = F(2, 10, 0, 10, 15);
        Assert.Single(ReglasComunidad.ElegirInvitados(actividad, 3, [P(1, F(2, 9, 45, 10, 30))]));
        Assert.Empty(ReglasComunidad.ElegirInvitados(actividad, 3, [P(1, F(2, 10, 5, 10, 30))]));
    }

    [Fact]
    public void Otro_dia_no_cuenta_y_con_dos_puentes_cuenta_el_mas_largo()
    {
        var actividad = F(2, 10, 0, 12, 0);
        var invitados = ReglasComunidad.ElegirInvitados(actividad, 3,
        [
            P(1, F(3, 10, 0, 12, 0)),
            P(2, F(2, 10, 0, 10, 40)), P(2, F(2, 11, 0, 12, 0)),
        ]);

        var unico = Assert.Single(invitados);
        Assert.Equal(2, unico.UsuarioId);
        Assert.Equal(60, unico.MinutosEnComun);
    }

    [Fact]
    public void En_empate_el_orden_es_siempre_el_mismo()
    {
        var actividad = F(2, 10, 0, 11, 0);
        var invitados = ReglasComunidad.ElegirInvitados(actividad, 2, [P(9, actividad), P(4, actividad), P(7, actividad)]);
        Assert.Equal([4, 7], invitados.Select(i => i.UsuarioId));
    }

    [Theory]
    [InlineData(2026, 11, 2, 1)]
    [InlineData(2026, 11, 3, 2)]
    [InlineData(2026, 11, 7, 6)]
    [InlineData(2026, 11, 8, 7)]
    public void El_dia_se_cuenta_como_en_horario_y_puente(int a, int m, int d, int esperado)
        => Assert.Equal(esperado, ReglasComunidad.DiaSemana(new DateTime(a, m, d)));

    [Fact]
    public void El_aviso_dice_quien_invita_que_y_cuando()
    {
        var mensaje = ReglasComunidad.MensajeInvitacion("Camila Torres", "jugar cartas", new TimeOnly(10, 30), new TimeOnly(11, 30));
        Assert.Equal("Camila te invita a «jugar cartas» hoy de 10:30 a 11:30. Coincide con tu tiempo libre.", mensaje);
    }

    [Fact]
    public void Un_aviso_largo_acorta_la_actividad_y_no_pasa_de_200()
    {
        var mensaje = ReglasComunidad.MensajeInvitacion(new string('N', 80), new string('x', 100), new TimeOnly(10, 30), new TimeOnly(11, 30));

        Assert.True(mensaje.Length <= ReglasComunidad.MaximoLargoMensaje);
        Assert.Contains("…»", mensaje);
        Assert.Contains("de 10:30 a 11:30", mensaje);
    }

    [Fact]
    public void El_mensaje_de_invitados_concuerda_en_numero()
    {
        Assert.Equal("Invitamos a 1 compañero libre a esa hora.", ReglasComunidad.MensajeInvitados(1));
        Assert.Equal("Invitamos a 3 compañeros libres a esa hora.", ReglasComunidad.MensajeInvitados(3));
    }
}

public class ComunidadServiceTests
{
    private static readonly DateTimeOffset AhoraUtc = new(2026, 11, 3, 14, 0, 0, TimeSpan.Zero);
    private const int Yo = 7;

    private sealed class RelojFijo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => AhoraUtc;
    }

    internal sealed class RepositorioFalso : IComunidadRepository
    {
        public Usuario Usuario { get; } = new()
        {
            Id = Yo, Nombre = "Camila Torres", Rol = "ESTUDIANTE", Activo = true, VisibleMatching = true,
            ZonaActualId = 3, ZonaActual = new Zona { Id = 3, Activa = true }
        };
        public ComunidadEfimera? Comunidad { get; set; }
        public List<int> YaDentro { get; } = [];
        public List<CandidatoComunidad> Candidatos { get; } = [];
        public ReglasMatching.Franja? UltimaBusqueda { get; private set; }
        public List<Participacion> Participaciones { get; } = [];
        public List<Notificacion> Avisos { get; } = [];
        public bool Falla { get; set; }

        public Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default) => Task.FromResult<Usuario?>(Usuario);
        public Task<ComunidadEfimera?> ObtenerComunidadAsync(int comunidadId, CancellationToken ct = default)
            => Task.FromResult(Comunidad?.Id == comunidadId ? Comunidad : null);
        public Task<List<int>> ListarParticipantesAsync(int comunidadId, CancellationToken ct = default) => Task.FromResult(YaDentro.ToList());

        public Task<List<CandidatoComunidad>> ListarCandidatosAsync(ComunidadEfimera comunidad, int zonaId, ReglasMatching.Franja actividad,
            CancellationToken ct = default)
        {
            UltimaBusqueda = actividad;
            return Task.FromResult(Candidatos);
        }

        public Task GuardarInvitacionesAsync(IReadOnlyList<Participacion> participaciones, IReadOnlyList<Notificacion> avisos,
            CancellationToken ct = default)
        {
            if (Falla) throw new InvalidOperationException("la base no responde");
            Participaciones.AddRange(participaciones);
            Avisos.AddRange(avisos);
            return Task.CompletedTask;
        }
    }

    internal static CandidatoComunidad Companero(int id, string nombre, string desde, string hasta, bool avisos = true)
        => new(id, nombre, "Ingeniería", avisos,
            [new Puente { UsuarioId = id, DiaSemana = 2, HoraInicio = TimeOnly.Parse(desde), HoraFin = TimeOnly.Parse(hasta) }]);

    private static ComunidadEfimera Actividad(short maximo = 4) => new()
    {
        Id = 12, CreadorId = Yo, Actividad = "jugar cartas", MaxParticipantes = maximo, Estado = ComunidadEfimera.EstadoActiva,
        Inicio = new DateTime(2026, 11, 3, 14, 30, 0, DateTimeKind.Utc), Fin = new DateTime(2026, 11, 3, 15, 30, 0, DateTimeKind.Utc)
    };

    private static (ComunidadService Servicio, RepositorioFalso Repo) Crear()
    {
        var repo = new RepositorioFalso();
        return (new ComunidadService(repo, new RelojFijo()), repo);
    }

    [Fact]
    public async Task Invita_a_los_compatibles_les_avisa_y_quien_crea_queda_dentro()
    {
        var (servicio, repo) = Crear();
        repo.Candidatos.Add(Companero(20, "Diego Mamani", "10:30", "12:00"));
        repo.Candidatos.Add(Companero(21, "Mateo Vargas", "09:30", "11:00"));

        var r = await servicio.InvitarAsync(Actividad());

        Assert.Equal(ComunidadService.EstadoInvitados, r.Estado);
        Assert.Equal("Invitamos a 2 compañeros libres a esa hora.", r.Mensaje);
        Assert.Equal(["Diego Mamani", "Mateo Vargas"], r.Invitados.Select(i => i.Nombre));
        Assert.Equal(("10:30", "11:00", 30), (r.Invitados[1].LibreDesde, r.Invitados[1].LibreHasta, r.Invitados[1].MinutosEnComun));

        Assert.Equal(new ReglasMatching.Franja(2, new TimeOnly(10, 30), new TimeOnly(11, 30)), repo.UltimaBusqueda);

        var mia = Assert.Single(repo.Participaciones, p => p.UsuarioId == Yo);
        Assert.Equal((Participacion.EstadoConfirmado, 12), (mia.Estado, mia.ComunidadId));
        Assert.NotNull(mia.RespondidoEn);
        Assert.All(repo.Participaciones.Where(p => p.UsuarioId != Yo), p =>
        {
            Assert.Equal(Participacion.EstadoPendiente, p.Estado);
            Assert.Null(p.RespondidoEn);
        });

        Assert.Equal([20, 21], repo.Avisos.Select(a => a.UsuarioId));
        Assert.All(repo.Avisos, a =>
        {
            Assert.Equal((Notificacion.TipoInvitacionComunidad, (int?)12), (a.Tipo, a.ComunidadId));
            Assert.Equal("Camila te invita a «jugar cartas» hoy de 10:30 a 11:30. Coincide con tu tiempo libre.", a.Mensaje);
        });
    }

    [Fact]
    public async Task Sin_coincidencias_no_invita_pero_quien_crea_queda_dentro()
    {
        var (servicio, repo) = Crear();
        repo.Candidatos.Add(Companero(20, "Diego Mamani", "11:15", "12:00"));

        var r = await servicio.InvitarAsync(Actividad());

        Assert.Equal((ComunidadService.EstadoSinCoincidencias, ReglasComunidad.MensajeSinCoincidencias), (r.Estado, r.Mensaje));
        Assert.Empty(r.Invitados);
        Assert.Equal([Yo], repo.Participaciones.Select(p => p.UsuarioId));
        Assert.Empty(repo.Avisos);
    }

    [Fact]
    public async Task Invita_solo_a_los_lugares_que_quedan()
    {
        var (servicio, repo) = Crear();
        repo.YaDentro.AddRange([Yo, 30]);
        for (var i = 0; i < 5; i++) repo.Candidatos.Add(Companero(20 + i, $"Compañero {i}", "10:00", "12:00"));

        var r = await servicio.InvitarAsync(Actividad(maximo: 4));

        Assert.Equal(2, r.Invitados.Count);
        Assert.DoesNotContain(repo.Participaciones, p => p.UsuarioId == Yo);
    }

    [Fact]
    public async Task Con_el_grupo_completo_no_busca()
    {
        var (servicio, repo) = Crear();
        repo.YaDentro.AddRange([Yo, 30]);

        var r = await servicio.InvitarAsync(Actividad(maximo: 2));

        Assert.Equal(ComunidadService.EstadoCompleta, r.Estado);
        Assert.Null(repo.UltimaBusqueda);
    }

    [Fact]
    public async Task Quien_apago_los_avisos_queda_invitado_pero_sin_aviso()
    {
        var (servicio, repo) = Crear();
        repo.Candidatos.Add(Companero(20, "Diego Mamani", "10:30", "12:00", avisos: false));
        repo.Candidatos.Add(Companero(21, "Lucía Rojas", "10:30", "12:00"));

        await servicio.InvitarAsync(Actividad());

        Assert.Contains(repo.Participaciones, p => p.UsuarioId == 20);
        Assert.Equal([21], repo.Avisos.Select(a => a.UsuarioId));
    }

    [Fact]
    public async Task Sin_campus_o_sin_aceptar_el_matching_no_busca()
    {
        var (servicio, repo) = Crear();
        repo.Candidatos.Add(Companero(20, "Diego Mamani", "10:30", "12:00"));

        repo.Usuario.ZonaActualId = null;
        repo.Usuario.ZonaActual = null;
        Assert.Equal(ComunidadService.EstadoSinZona, (await servicio.InvitarAsync(Actividad())).Estado);

        repo.Usuario.ZonaActualId = 3;
        repo.Usuario.ZonaActual = new Zona { Id = 3, Activa = true };
        repo.Usuario.VisibleMatching = false;
        Assert.Equal(ComunidadService.EstadoNoVisible, (await servicio.InvitarAsync(Actividad())).Estado);

        Assert.Null(repo.UltimaBusqueda);
        Assert.Empty(repo.Avisos);
    }

    [Fact]
    public async Task Volver_a_buscar_solo_en_una_actividad_propia_que_no_termino()
    {
        var (servicio, repo) = Crear();
        repo.Comunidad = Actividad();
        repo.Candidatos.Add(Companero(20, "Diego Mamani", "10:30", "12:00"));

        Assert.Equal(ComunidadService.EstadoInvitados, (await servicio.InvitarDeNuevoAsync(Yo, 12)).Estado);

        var ajena = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.InvitarDeNuevoAsync(99, 12));
        Assert.Equal("NO_ENCONTRADO", ajena.Codigo);
        var noExiste = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.InvitarDeNuevoAsync(Yo, 404));
        Assert.Equal("NO_ENCONTRADO", noExiste.Codigo);

        repo.Comunidad.Fin = AhoraUtc.UtcDateTime.AddMinutes(-1);
        var terminada = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.InvitarDeNuevoAsync(Yo, 12));
        Assert.Equal("VALIDACION", terminada.Codigo);
    }
}

public class CreacionActividadConInvitacionTests
{
    private sealed class RelojFijo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 3, 14, 0, 0, TimeSpan.Zero);
    }

    private sealed class ActividadesFalsas : IActividadRepository
    {
        public Task<LogAuditoriaRag?> ObtenerExtraccionAsync(long consultaId, int usuarioId, CancellationToken ct = default)
            => Task.FromResult<LogAuditoriaRag?>(new LogAuditoriaRag
            {
                Id = consultaId, UsuarioId = usuarioId, Tipo = LogAuditoriaRag.TipoExtraccionComunidad, Estado = LogAuditoriaRag.EstadoOk
            });
        public Task<bool> ConsultaYaUsadaAsync(long consultaId, CancellationToken ct = default) => Task.FromResult(false);
        public Task AgregarAsync(ComunidadEfimera comunidad, CancellationToken ct = default)
        {
            comunidad.Id = 12;
            return Task.CompletedTask;
        }
    }

    private static readonly CrearActividadRequest Pedido = new("quiero jugar cartas", "jugar cartas", "10:30", 60, 4, 58);

    [Fact]
    public async Task Al_crear_la_actividad_se_invita_a_los_compatibles()
    {
        var comunidades = new ComunidadServiceTests.RepositorioFalso();
        comunidades.Candidatos.Add(ComunidadServiceTests.Companero(20, "Diego Mamani", "10:30", "12:00"));
        var reloj = new RelojFijo();
        var servicio = new CreacionActividadService(new ActividadesFalsas(), reloj, new ComunidadService(comunidades, reloj));

        var creada = await servicio.CrearAsync(7, Pedido);

        Assert.Equal(ComunidadService.EstadoInvitados, creada.Invitacion!.Estado);
        Assert.Equal(["Diego Mamani"], creada.Invitacion.Invitados.Select(i => i.Nombre));
        Assert.All(comunidades.Participaciones, p => Assert.Equal(12, p.ComunidadId));
        Assert.Single(comunidades.Avisos);
    }

    [Fact]
    public async Task Si_invitar_falla_la_actividad_igual_queda_creada()
    {
        var comunidades = new ComunidadServiceTests.RepositorioFalso { Falla = true };
        comunidades.Candidatos.Add(ComunidadServiceTests.Companero(20, "Diego Mamani", "10:30", "12:00"));
        var reloj = new RelojFijo();
        var servicio = new CreacionActividadService(new ActividadesFalsas(), reloj, new ComunidadService(comunidades, reloj));

        var creada = await servicio.CrearAsync(7, Pedido);

        Assert.Equal(12, creada.Id);
        Assert.Equal(ComunidadService.EstadoError, creada.Invitacion!.Estado);
    }

    [Fact]
    public async Task Sin_invitador_la_creacion_de_HU10_sigue_igual()
    {
        var creada = await new CreacionActividadService(new ActividadesFalsas(), new RelojFijo()).CrearAsync(7, Pedido);
        Assert.Null(creada.Invitacion);
    }
}
