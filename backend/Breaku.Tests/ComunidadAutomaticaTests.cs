using Breaku.Application.Actividades;
using Breaku.Application.Comunidades;
using Breaku.Application.Horarios;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Tests;

/// <summary>HU13: a quién se invita y qué dice el aviso.</summary>
public class ReglasComunidadTests
{
    private static ReglasMatching.Franja F(int dia, int h1, int m1, int h2, int m2) => new(dia, new TimeOnly(h1, m1), new TimeOnly(h2, m2));
    private static ReglasMatching.PuenteDe P(int usuario, ReglasMatching.Franja f) => new(usuario, f);

    [Fact]
    public void Invita_primero_a_quien_comparte_mas_tiempo()
    {
        var actividad = F(2, 10, 30, 11, 30);
        var invitados = ReglasComunidad.ElegirInvitados(actividad, 4,
        [
            P(1, F(2, 11, 0, 12, 0)),     // 30 min
            P(2, F(2, 9, 30, 11, 30)),    // 60 min
            P(3, F(2, 10, 0, 11, 15)),    // 45 min
        ]);

        Assert.Equal([2, 3, 1], invitados.Select(i => i.UsuarioId));
        Assert.Equal(new TimeOnly(10, 30), invitados[0].EnComun.Inicio);
        Assert.Equal(new TimeOnly(11, 30), invitados[0].EnComun.Fin);
        Assert.Equal(60, invitados[0].MinutosEnComun);
    }

    [Fact]
    public void Invita_uno_por_plaza_contando_a_quien_la_crea()
    {
        var actividad = F(2, 10, 0, 11, 0);
        var otros = Enumerable.Range(1, 6).Select(i => P(i, F(2, 10, 0, 11, 0)));

        Assert.Equal(1, ReglasComunidad.ElegirInvitados(actividad, 2, otros).Count);
        Assert.Equal(3, ReglasComunidad.ElegirInvitados(actividad, 4, otros).Count);
        Assert.Equal(6, ReglasComunidad.ElegirInvitados(actividad, 30, otros).Count);
    }

    [Fact]
    public void Con_menos_de_30_minutos_en_comun_no_se_invita()
    {
        var actividad = F(2, 10, 0, 11, 0);
        Assert.Empty(ReglasComunidad.ElegirInvitados(actividad, 4, [P(1, F(2, 10, 40, 12, 0))]));   // 20 min
    }

    [Fact]
    public void Una_actividad_corta_basta_con_compartirla_entera()
    {
        var actividad = F(2, 10, 0, 10, 15);   // 15 min
        Assert.Single(ReglasComunidad.ElegirInvitados(actividad, 4, [P(1, F(2, 9, 45, 10, 30))]));
        Assert.Empty(ReglasComunidad.ElegirInvitados(actividad, 4, [P(1, F(2, 10, 5, 10, 30))]));      // le faltan 5 min
    }

    [Fact]
    public void Otro_dia_no_cuenta_y_con_dos_puentes_cuenta_el_mas_largo()
    {
        var actividad = F(2, 10, 0, 12, 0);
        var invitados = ReglasComunidad.ElegirInvitados(actividad, 4,
        [
            P(1, F(3, 10, 0, 12, 0)),                                // miércoles: no cuenta
            P(2, F(2, 10, 0, 10, 40)), P(2, F(2, 11, 0, 12, 0)),     // 40 y 60 min: cuenta 60
        ]);

        var unico = Assert.Single(invitados);
        Assert.Equal(2, unico.UsuarioId);
        Assert.Equal(60, unico.MinutosEnComun);
    }

    [Fact]
    public void En_empate_el_orden_es_siempre_el_mismo()
    {
        var actividad = F(2, 10, 0, 11, 0);
        var invitados = ReglasComunidad.ElegirInvitados(actividad, 3, [P(9, actividad), P(4, actividad), P(7, actividad)]);
        Assert.Equal([4, 7], invitados.Select(i => i.UsuarioId));
    }

    [Theory]
    [InlineData(2026, 11, 2, 1)]   // lunes
    [InlineData(2026, 11, 3, 2)]   // martes
    [InlineData(2026, 11, 7, 6)]   // sábado
    [InlineData(2026, 11, 8, 7)]   // domingo
    public void El_dia_se_cuenta_como_en_horario_y_puente(int a, int m, int d, int esperado)
        => Assert.Equal(esperado, ReglasComunidad.DiaSemana(new DateTime(a, m, d)));

    [Fact]
    public void La_hora_de_Bolivia_se_guarda_en_UTC()
    {
        var utc = ReglasComunidad.AUtc(new DateTime(2026, 11, 3, 20, 30, 0));
        Assert.Equal(new DateTime(2026, 11, 4, 0, 30, 0), utc);
        Assert.Equal(DateTimeKind.Utc, utc.Kind);
    }

    [Fact]
    public void El_aviso_dice_quien_invita_que_y_cuando()
    {
        var mensaje = ReglasComunidad.MensajeInvitacion("Camila Torres", "jugar cartas", new TimeOnly(10, 30), new TimeOnly(11, 30));
        Assert.Equal("Camila te invita a «jugar cartas» hoy de 10:30 a 11:30. Coincide con tu tiempo libre.", mensaje);
    }

    [Fact]
    public void Un_aviso_largo_acorta_la_actividad_y_no_pasa_de_200()
    {
        var actividad = new string('x', 100);
        var mensaje = ReglasComunidad.MensajeInvitacion(new string('N', 80), actividad, new TimeOnly(10, 30), new TimeOnly(11, 30));

        Assert.True(mensaje.Length <= ReglasComunidad.MaximoLargoMensaje);
        Assert.Contains("…»", mensaje);
        Assert.Contains("de 10:30 a 11:30", mensaje);
    }
}

/// <summary>HU13: el caso de uso completo, con un repositorio falso.</summary>
public class ComunidadServiceTests
{
    // Martes 3 de noviembre de 2026, 10:00 en Bolivia = 14:00 UTC.
    private static readonly DateTimeOffset AhoraUtc = new(2026, 11, 3, 14, 0, 0, TimeSpan.Zero);
    private const int Yo = 7;
    private const long Consulta = 58;

    private sealed class RelojFijo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => AhoraUtc;
    }

    private sealed class RepositorioFalso : IComunidadRepository
    {
        public Usuario Usuario { get; set; } = new()
        {
            Id = Yo, Nombre = "Camila Torres", Rol = "ESTUDIANTE", Activo = true, VisibleMatching = true,
            ZonaActualId = 3, ZonaActual = new Zona { Id = 3, Activa = true }
        };
        public LogAuditoriaRag? Log { get; set; } = new()
        {
            Id = Consulta, UsuarioId = Yo, Tipo = LogAuditoriaRag.TipoExtraccionComunidad, Estado = LogAuditoriaRag.EstadoOk,
            CreatedAt = AhoraUtc.UtcDateTime.AddMinutes(-1)
        };
        public bool YaCreada { get; set; }
        public bool Ocupado { get; set; }
        public List<CandidatoComunidad> Candidatos { get; } = [];

        public (int Dia, TimeOnly Inicio, TimeOnly Fin, DateTime InicioUtc)? UltimaBusqueda { get; private set; }
        public ComunidadEfimera? Creada { get; private set; }
        public List<Participacion> Participaciones { get; } = [];
        public List<Notificacion> Avisos { get; } = [];

        public Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default) => Task.FromResult<Usuario?>(Usuario);
        public Task<LogAuditoriaRag?> ObtenerExtraccionAsync(long consultaId, CancellationToken ct = default)
            => Task.FromResult(Log?.Id == consultaId ? Log : null);
        public Task<bool> ExisteComunidadDeConsultaAsync(long consultaId, CancellationToken ct = default) => Task.FromResult(YaCreada);
        public Task<bool> EstaEnOtraComunidadAsync(int usuarioId, DateTime inicioUtc, DateTime finUtc, CancellationToken ct = default)
            => Task.FromResult(Ocupado);

        public Task<List<CandidatoComunidad>> ListarCandidatosAsync(int usuarioId, int zonaId, ReglasMatching.Franja actividad,
            DateTime inicioUtc, DateTime finUtc, CancellationToken ct = default)
        {
            UltimaBusqueda = (actividad.DiaSemana, actividad.Inicio, actividad.Fin, inicioUtc);
            return Task.FromResult(Candidatos);
        }

        public Task CrearAsync(ComunidadEfimera comunidad, IReadOnlyList<Participacion> participaciones, IReadOnlyList<Notificacion> avisos,
            CancellationToken ct = default)
        {
            comunidad.Id = 12;
            Creada = comunidad;
            Participaciones.AddRange(participaciones);
            Avisos.AddRange(avisos);
            return Task.CompletedTask;
        }
    }

    private static CandidatoComunidad Companero(int id, string nombre, string desde, string hasta, bool avisos = true)
        => new(id, nombre, "Ingeniería", avisos,
            [new Puente { UsuarioId = id, DiaSemana = 2, HoraInicio = TimeOnly.Parse(desde), HoraFin = TimeOnly.Parse(hasta) }]);

    private static CrearComunidadRequest Pedido(string? hora = "10:30", int? duracion = 60, int? personas = 4, long? consulta = Consulta,
        string? actividad = "jugar cartas")
        => new(consulta, "Quiero jugar cartas", new ActividadExtraidaDto(actividad, hora, duracion, personas));

    private static (ComunidadService Servicio, RepositorioFalso Repo) Crear()
    {
        var repo = new RepositorioFalso();
        return (new ComunidadService(repo, new RelojFijo()), repo);
    }

    [Fact]
    public async Task Con_coincidencias_crea_la_comunidad_invita_y_avisa()
    {
        var (servicio, repo) = Crear();
        repo.Candidatos.Add(Companero(20, "Diego Mamani", "10:30", "12:00"));
        repo.Candidatos.Add(Companero(21, "Mateo Vargas", "09:30", "11:00"));

        var r = await servicio.CrearAsync(Yo, Pedido());

        Assert.Equal(ComunidadService.EstadoCreada, r.Estado);
        Assert.Equal("¡Grupo armado! Invitamos a 2 compañeros libres a esa hora.", r.Mensaje);
        Assert.Equal(12, r.Comunidad!.Id);
        Assert.Equal(["Diego Mamani", "Mateo Vargas"], r.Comunidad.Invitados.Select(i => i.Nombre));
        Assert.Equal(("10:30", "11:00", 30), (r.Comunidad.Invitados[1].LibreDesde, r.Comunidad.Invitados[1].LibreHasta, r.Comunidad.Invitados[1].MinutosEnComun));

        // Se buscó el martes de 10:30 a 11:30 (hora de Bolivia) y se guardó en UTC.
        Assert.Equal((2, new TimeOnly(10, 30), new TimeOnly(11, 30), new DateTime(2026, 11, 3, 14, 30, 0)), repo.UltimaBusqueda);
        var c = repo.Creada!;
        Assert.Equal((Consulta, "jugar cartas", "Quiero jugar cartas", (short)4), (c.LogId, c.Actividad, c.TextoOriginal, c.MaxParticipantes));
        Assert.Equal(new DateTime(2026, 11, 3, 14, 30, 0), c.Inicio);
        Assert.Equal(new DateTime(2026, 11, 3, 15, 30, 0), c.Fin);

        // Quien la crea queda confirmado (con fecha de respuesta) y los demás, pendientes.
        var mia = Assert.Single(repo.Participaciones, p => p.UsuarioId == Yo);
        Assert.Equal(Participacion.EstadoConfirmado, mia.Estado);
        Assert.NotNull(mia.RespondidoEn);
        Assert.All(repo.Participaciones.Where(p => p.UsuarioId != Yo), p =>
        {
            Assert.Equal(Participacion.EstadoPendiente, p.Estado);
            Assert.Null(p.RespondidoEn);
        });

        Assert.Equal([20, 21], repo.Avisos.Select(a => a.UsuarioId));
        Assert.All(repo.Avisos, a =>
        {
            Assert.Equal(Notificacion.TipoInvitacionComunidad, a.Tipo);
            Assert.Equal("Camila te invita a «jugar cartas» hoy de 10:30 a 11:30. Coincide con tu tiempo libre.", a.Mensaje);
        });
    }

    [Fact]
    public async Task Sin_coincidencias_no_crea_nada()
    {
        var (servicio, repo) = Crear();
        repo.Candidatos.Add(Companero(20, "Diego Mamani", "11:15", "12:00"));   // 15 min: no alcanza

        var r = await servicio.CrearAsync(Yo, Pedido());

        Assert.Equal(ComunidadService.EstadoSinCoincidencias, r.Estado);
        Assert.Equal(ReglasComunidad.MensajeSinCoincidencias, r.Mensaje);
        Assert.Null(r.Comunidad);
        Assert.Null(repo.Creada);
    }

    [Fact]
    public async Task Invita_solo_a_tantos_como_plazas_tiene_el_grupo()
    {
        var (servicio, repo) = Crear();
        for (var i = 0; i < 5; i++) repo.Candidatos.Add(Companero(20 + i, $"Compañero {i}", "10:00", "12:00"));

        var r = await servicio.CrearAsync(Yo, Pedido(personas: 2));

        Assert.Equal("¡Grupo armado! Invitamos a 1 compañero libre a esa hora.", r.Mensaje);
        Assert.Equal(2, repo.Participaciones.Count);   // quien la crea + 1
    }

    [Fact]
    public async Task Quien_apago_los_avisos_queda_invitado_pero_sin_aviso()
    {
        var (servicio, repo) = Crear();
        repo.Candidatos.Add(Companero(20, "Diego Mamani", "10:30", "12:00", avisos: false));
        repo.Candidatos.Add(Companero(21, "Lucía Rojas", "10:30", "12:00"));

        await servicio.CrearAsync(Yo, Pedido());

        Assert.Contains(repo.Participaciones, p => p.UsuarioId == 20);
        Assert.Equal([21], repo.Avisos.Select(a => a.UsuarioId));
    }

    [Fact]
    public async Task Sin_campus_o_sin_aceptar_el_matching_no_crea_nada()
    {
        var (servicio, repo) = Crear();
        repo.Candidatos.Add(Companero(20, "Diego Mamani", "10:30", "12:00"));

        repo.Usuario.ZonaActualId = null;
        repo.Usuario.ZonaActual = null;
        Assert.Equal(ComunidadService.EstadoSinZona, (await servicio.CrearAsync(Yo, Pedido())).Estado);

        repo.Usuario.ZonaActualId = 3;
        repo.Usuario.ZonaActual = new Zona { Id = 3, Activa = true };
        repo.Usuario.VisibleMatching = false;
        Assert.Equal(ComunidadService.EstadoNoVisible, (await servicio.CrearAsync(Yo, Pedido())).Estado);

        Assert.Null(repo.Creada);
        Assert.Null(repo.UltimaBusqueda);
    }

    [Theory]
    [InlineData("07:00", 60, 4)]    // antes de las 07:15
    [InlineData("09:00", 60, 4)]    // ya pasó (son las 10:00)
    [InlineData("20:30", 60, 4)]    // terminaría después de las 21:00
    [InlineData("10:30", 5, 4)]     // menos de 10 minutos
    [InlineData("10:30", 60, 1)]    // menos de 2 personas
    [InlineData("10:30", 60, 31)]   // más de 30 personas
    [InlineData(null, 60, 4)]       // sin hora
    [InlineData("10:30", null, 4)]  // sin duración
    public async Task Datos_no_validos_se_rechazan(string? hora, int? duracion, int? personas)
    {
        var (servicio, repo) = Crear();
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.CrearAsync(Yo, Pedido(hora, duracion, personas)));
        Assert.Equal("VALIDACION", ex.Codigo);
        Assert.Null(repo.Creada);
    }

    [Fact]
    public async Task Sin_actividad_extraida_se_rechaza()
    {
        var (servicio, _) = Crear();
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.CrearAsync(Yo, new CrearComunidadRequest(null, null, null)));
        Assert.Equal("VALIDACION", ex.Codigo);
    }

    public static TheoryData<string> ExtraccionesNoValidas => ["otro", "tipo", "incompleta", "ayer", "inexistente"];

    [Theory]
    [MemberData(nameof(ExtraccionesNoValidas))]
    public async Task La_extraccion_tiene_que_ser_mia_de_hoy_y_completa(string caso)
    {
        var (servicio, repo) = Crear();
        switch (caso)
        {
            case "otro": repo.Log!.UsuarioId = 99; break;
            case "tipo": repo.Log!.Tipo = LogAuditoriaRag.TipoRecomendacion; break;
            case "incompleta": repo.Log!.Estado = LogAuditoriaRag.EstadoIncompleto; break;
            case "ayer": repo.Log!.CreatedAt = AhoraUtc.UtcDateTime.AddDays(-1); break;
            case "inexistente": repo.Log = null; break;
        }

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.CrearAsync(Yo, Pedido()));
        Assert.Equal("NO_ENCONTRADO", ex.Codigo);
    }

    [Fact]
    public async Task La_misma_extraccion_no_crea_dos_comunidades()
    {
        var (servicio, repo) = Crear();
        repo.YaCreada = true;
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.CrearAsync(Yo, Pedido()));
        Assert.Equal("YA_CREADA", ex.Codigo);
    }

    [Fact]
    public async Task Si_ya_estoy_en_otra_comunidad_a_esa_hora_se_rechaza()
    {
        var (servicio, repo) = Crear();
        repo.Ocupado = true;
        repo.Candidatos.Add(Companero(20, "Diego Mamani", "10:30", "12:00"));

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.CrearAsync(Yo, Pedido()));
        Assert.Equal("EN_OTRA_COMUNIDAD", ex.Codigo);
        Assert.Null(repo.Creada);
    }

    [Fact]
    public async Task El_texto_original_se_sanea_y_si_falta_se_usa_la_actividad()
    {
        var (servicio, repo) = Crear();
        repo.Candidatos.Add(Companero(20, "Diego Mamani", "10:30", "12:00"));

        await servicio.CrearAsync(Yo, Pedido() with { Texto = "   " });
        Assert.Equal("jugar cartas", repo.Creada!.TextoOriginal);
    }
}
