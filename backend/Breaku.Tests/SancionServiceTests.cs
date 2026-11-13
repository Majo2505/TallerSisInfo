using Breaku.Application.Horarios;
using Breaku.Application.Sanciones;
using Breaku.Application.TiempoReal;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Tests;

public class SancionServiceTests
{
    private const int Admin = 1;
    private const int Ana = 10;
    private static readonly DateTimeOffset Ahora = new(2026, 11, 13, 15, 0, 0, TimeSpan.Zero);

    private sealed class Reloj : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Ahora;
    }

    private sealed class Tx : ITransaccion
    {
        public int Ejecuciones { get; private set; }
        public bool Abierta { get; private set; }

        public async Task<T> EjecutarAsync<T>(Func<Task<T>> trabajo, CancellationToken ct = default)
        {
            if (Abierta) return await trabajo();
            Abierta = true;
            Ejecuciones++;
            try { return await trabajo(); }
            finally { Abierta = false; }
        }
    }

    private sealed class Fila
    {
        public int Id;
        public int UsuarioId;
        public string Tipo = "";
        public string Motivo = "";
        public int? ReporteId;
        public int CreadoPor;
        public DateTime CreadoEn;
        public DateTime? Hasta;
        public DateTime? LevantadaEn;
        public int? LevantadaPor;
    }

    private sealed class RepoFalso(Tx tx) : ISancionRepository
    {
        public Dictionary<int, DatosUsuario> Usuarios { get; } = new()
        {
            [Admin] = new(Admin, "Admin", "admin@ucb.edu.bo", null, null, "ADMIN", true, new DateTime(2026, 1, 1)),
            [2] = new(2, "Otro Admin", "admin2@ucb.edu.bo", null, null, "ADMIN", true, new DateTime(2026, 1, 1)),
            [Ana] = new(Ana, "Ana", "ana@ucb.edu.bo", "Sistemas", 5, "ESTUDIANTE", true, new DateTime(2026, 2, 1))
        };

        public List<Fila> Sanciones { get; } = [];
        public List<(int Usuario, int Sancion, string Mensaje)> Avisos { get; } = [];
        public HashSet<int> Reportes { get; } = [77];
        public bool TodoDentroDeTransaccion { get; private set; } = true;
        private int _siguiente = 1;

        private void Dentro()
        {
            if (!tx.Abierta) TodoDentroDeTransaccion = false;
        }

        public Task<List<DatosUsuario>> ListarEstudiantesAsync(string? buscar, int limite, CancellationToken ct = default)
            => Task.FromResult(Usuarios.Values.Where(u => u.Rol == "ESTUDIANTE")
                .Where(u => string.IsNullOrEmpty(buscar) || u.Nombre.Contains(buscar, StringComparison.OrdinalIgnoreCase)
                                                          || u.Correo.Contains(buscar, StringComparison.OrdinalIgnoreCase))
                .Take(limite).ToList());

        public Task<List<RegistroDeUsuario>> ListarRegistrosAsync(IReadOnlyCollection<int> usuarioIds, CancellationToken ct = default)
            => Task.FromResult(Sanciones.Where(s => usuarioIds.Contains(s.UsuarioId)).Select(s => new RegistroDeUsuario(s.UsuarioId, Reg(s))).ToList());

        public Task<DatosUsuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default)
            => Task.FromResult(Usuarios.GetValueOrDefault(usuarioId));

        public Task<List<SancionDetalle>> ListarSancionesAsync(int usuarioId, CancellationToken ct = default)
            => Task.FromResult(Sanciones.Where(s => s.UsuarioId == usuarioId).Select(Detalle).ToList());

        public Task<SancionDetalle?> ObtenerSancionAsync(int usuarioId, int sancionId, CancellationToken ct = default)
            => Task.FromResult(Sanciones.Where(s => s.UsuarioId == usuarioId && s.Id == sancionId).Select(Detalle).FirstOrDefault());

        public Task<bool> ReporteExisteAsync(int reporteId, CancellationToken ct = default) => Task.FromResult(Reportes.Contains(reporteId));

        public Task BloquearUsuarioAsync(int usuarioId, CancellationToken ct = default)
        {
            Dentro();
            return Task.CompletedTask;
        }

        public Task<int> AgregarAsync(int usuarioId, ReglasSancion.SancionValida sancion, int? reporteId, int adminId, DateTime ahoraUtc,
            CancellationToken ct = default)
        {
            Dentro();
            var fila = new Fila
            {
                Id = _siguiente++, UsuarioId = usuarioId, Tipo = sancion.Tipo, Motivo = sancion.Motivo, ReporteId = reporteId,
                CreadoPor = adminId, CreadoEn = ahoraUtc, Hasta = sancion.Hasta
            };
            Sanciones.Add(fila);
            return Task.FromResult(fila.Id);
        }

        public Task<bool> LevantarAsync(int sancionId, int adminId, DateTime ahoraUtc, CancellationToken ct = default)
        {
            var fila = Sanciones.Single(s => s.Id == sancionId);
            if (fila.LevantadaEn is not null) return Task.FromResult(false);
            fila.LevantadaEn = ahoraUtc;
            fila.LevantadaPor = adminId;
            return Task.FromResult(true);
        }

        public Task AgregarAvisoAdvertenciaAsync(int usuarioId, int sancionId, string mensaje, DateTime ahoraUtc, CancellationToken ct = default)
        {
            Dentro();
            Avisos.Add((usuarioId, sancionId, mensaje));
            return Task.CompletedTask;
        }

        public Task<EstadoSesion?> ObtenerEstadoSesionAsync(int usuarioId, DateTime ahoraUtc, CancellationToken ct = default)
            => throw new NotSupportedException();

        public void Anonimizar(int usuarioId) => Usuarios[usuarioId] = Usuarios[usuarioId] with { Nombre = "Cuenta dada de baja", Carrera = null, Activo = false };

        private static ReglasSancion.Registro Reg(Fila s) => new(s.Id, s.Tipo, s.Motivo, s.CreadoEn, s.Hasta, s.LevantadaEn);

        private SancionDetalle Detalle(Fila s) => new(Reg(s), s.ReporteId, Usuarios[s.CreadoPor].Nombre,
            s.LevantadaPor is int id ? Usuarios[id].Nombre : null);
    }

    private sealed class PurgaFalsa(RepoFalso repo) : IPurgaRepository
    {
        public List<(int Usuario, bool Eliminar)> Purgas { get; } = [];
        public int Anonimizaciones { get; private set; }

        public Task<List<ReglasPurga.ComunidadCreada>> ListarComunidadesCreadasAsync(int usuarioId, CancellationToken ct = default) => Task.FromResult(new List<ReglasPurga.ComunidadCreada>());
        public Task CambiarCreadorAsync(int comunidadId, int nuevoCreadorId, DateTime ahoraUtc, CancellationToken ct = default) => Task.CompletedTask;
        public Task EliminarComunidadAsync(int comunidadId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<List<(int ComunidadId, int Confirmados)>> EliminarParticipacionesAsync(int usuarioId, CancellationToken ct = default) => Task.FromResult(new List<(int, int)>());
        public Task EliminarNotificacionesAsync(int usuarioId, CancellationToken ct = default) { Purgas.Add((usuarioId, false)); return Task.CompletedTask; }
        public Task EliminarEventosAsync(int usuarioId, CancellationToken ct = default) => Task.CompletedTask;
        public Task EliminarReportesAsync(int usuarioId, CancellationToken ct = default) => Task.CompletedTask;
        public Task EliminarRecomendacionesAsync(int usuarioId, CancellationToken ct = default) => Task.CompletedTask;
        public Task EliminarLugaresRecuperadosAsync(int usuarioId, CancellationToken ct = default) => Task.CompletedTask;
        public Task EliminarLogsAuditoriaAsync(int usuarioId, CancellationToken ct = default) => Task.CompletedTask;
        public Task EliminarPuentesAsync(int usuarioId, CancellationToken ct = default) => Task.CompletedTask;
        public Task EliminarHorariosAsync(int usuarioId, CancellationToken ct = default) => Task.CompletedTask;
        public Task EliminarHistorialAsync(int usuarioId, CancellationToken ct = default) => Task.CompletedTask;
        public Task EliminarSancionesAsync(int usuarioId, CancellationToken ct = default) => Task.CompletedTask;
        public Task EliminarUsuarioAsync(int usuarioId, CancellationToken ct = default) => Task.CompletedTask;

        public Task AnonimizarUsuarioAsync(int usuarioId, string hashInutilizable, DateTime ahoraUtc, CancellationToken ct = default)
        {
            Anonimizaciones++;
            repo.Anonimizar(usuarioId);
            return Task.CompletedTask;
        }
    }

    private sealed class Validador : IValidadorSesion
    {
        public List<int> Invalidados { get; } = [];
        public Task<ResultadoSesion> ValidarAsync(int usuarioId, CancellationToken ct = default) => Task.FromResult(new ResultadoSesion(true));
        public void Invalidar(int usuarioId) => Invalidados.Add(usuarioId);
    }

    private sealed class Vivo : ITiempoReal
    {
        public List<int> Avisados { get; } = [];
        public Task AvisosCambiaronAsync(IReadOnlyCollection<int> usuarioIds, CancellationToken ct = default) { Avisados.AddRange(usuarioIds); return Task.CompletedTask; }
        public Task ComunidadCreadaAsync(int comunidadId, CancellationToken ct = default) => Task.CompletedTask;
        public Task AvisosDeComunidadAsync(int comunidadId, CancellationToken ct = default) => Task.CompletedTask;
        public Task ComunidadActualizadaAsync(int comunidadId, string estado, int confirmados, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed record Entorno(SancionService Servicio, RepoFalso Repo, PurgaFalsa Purga, Tx Tx, Validador Validador, Vivo Vivo);

    private static Entorno Armar()
    {
        var tx = new Tx();
        var repo = new RepoFalso(tx);
        var purgaRepo = new PurgaFalsa(repo);
        var vivo = new Vivo();
        var validador = new Validador();
        var purga = new PurgaUsuarioService(purgaRepo, tx, vivo, new Reloj());
        return new Entorno(new SancionService(repo, purga, tx, vivo, validador, new Reloj()), repo, purgaRepo, tx, validador, vivo);
    }

    private static SancionarUsuarioDto Pide(string tipo, string? motivo = "Lenguaje ofensivo", DateTime? hasta = null, int? reporte = null)
        => new(tipo, motivo, hasta, reporte);

    private static async Task<ReglaNegocioException> Rechaza(Task tarea)
        => await Assert.ThrowsAsync<ReglaNegocioException>(() => tarea);

    // ---------- crear

    [Fact]
    public async Task Sin_motivo_devuelve_un_error_de_validacion_y_no_guarda_nada()
    {
        var e = Armar();

        var error = await Rechaza(e.Servicio.AplicarAsync(Admin, Ana, Pide("ADVERTENCIA", "  ")));

        Assert.Equal("VALIDACION", error.Codigo);
        Assert.Empty(e.Repo.Sanciones);
    }

    [Fact]
    public async Task El_admin_no_se_sanciona_a_si_mismo()
    {
        var e = Armar();

        var error = await Rechaza(e.Servicio.AplicarAsync(Admin, Admin, Pide("ADVERTENCIA")));

        Assert.Equal("NO_SANCIONABLE", error.Codigo);
        Assert.Empty(e.Repo.Sanciones);
    }

    [Fact]
    public async Task El_admin_no_sanciona_a_otro_admin()
    {
        var e = Armar();

        Assert.Equal("NO_SANCIONABLE", (await Rechaza(e.Servicio.AplicarAsync(Admin, 2, Pide("BAJA")))).Codigo);
        Assert.Empty(e.Repo.Sanciones);
    }

    [Fact]
    public async Task Un_usuario_que_no_existe_da_NO_ENCONTRADO()
        => Assert.Equal("NO_ENCONTRADO", (await Rechaza(Armar().Servicio.AplicarAsync(Admin, 999, Pide("ADVERTENCIA")))).Codigo);

    [Fact]
    public async Task Un_reporte_de_origen_que_no_existe_no_vale()
    {
        var error = await Rechaza(Armar().Servicio.AplicarAsync(Admin, Ana, Pide("ADVERTENCIA", reporte: 5)));

        Assert.Equal("VALIDACION", error.Codigo);
    }

    [Fact]
    public async Task La_sancion_registra_quien_cuando_y_el_reporte_de_origen()
    {
        var e = Armar();

        var respuesta = await e.Servicio.AplicarAsync(Admin, Ana, Pide("ADVERTENCIA", reporte: 77));

        var fila = Assert.Single(e.Repo.Sanciones);
        Assert.Equal((Admin, Ahora.UtcDateTime, 77), (fila.CreadoPor, fila.CreadoEn, fila.ReporteId));
        Assert.Equal("Admin", respuesta.Sancion.CreadoPor);
        Assert.Equal(77, respuesta.Sancion.ReporteId);
    }

    [Fact]
    public async Task La_advertencia_notifica_al_estudiante_con_su_motivo_y_avisa_en_vivo()
    {
        var e = Armar();

        await e.Servicio.AplicarAsync(Admin, Ana, Pide("ADVERTENCIA", "Lenguaje ofensivo"));

        var aviso = Assert.Single(e.Repo.Avisos);
        Assert.Equal((Ana, 1, "Recibiste una advertencia: Lenguaje ofensivo"), aviso);
        Assert.Equal([Ana], e.Vivo.Avisados);
        Assert.Equal([Ana], e.Validador.Invalidados);
    }

    [Theory]
    [InlineData("SUSPENSION")]
    [InlineData("BAJA")]
    public async Task La_suspension_y_la_baja_no_notifican(string tipo)
    {
        var e = Armar();

        await e.Servicio.AplicarAsync(Admin, Ana, Pide(tipo));

        Assert.Empty(e.Repo.Avisos);
        Assert.Empty(e.Vivo.Avisados);
    }

    [Fact]
    public async Task La_suspension_sin_fecha_de_fin_se_guarda_con_siete_dias()
    {
        var e = Armar();

        await e.Servicio.AplicarAsync(Admin, Ana, Pide("SUSPENSION"));

        Assert.Equal(Ahora.UtcDateTime.AddDays(7), e.Repo.Sanciones.Single().Hasta);
    }

    [Fact]
    public async Task La_suspension_con_fecha_elegida_por_el_admin_la_respeta_y_deja_el_usuario_suspendido()
    {
        var e = Armar();
        var hasta = Ahora.UtcDateTime.AddDays(14);

        var respuesta = await e.Servicio.AplicarAsync(Admin, Ana, Pide("SUSPENSION", hasta: hasta));

        Assert.Equal(hasta, e.Repo.Sanciones.Single().Hasta);
        Assert.Equal("SUSPENDIDO", respuesta.EstadoUsuario);
        Assert.True(respuesta.Sancion.Vigente);
    }

    [Fact]
    public async Task No_se_encima_una_suspension_con_otra()
    {
        var e = Armar();
        await e.Servicio.AplicarAsync(Admin, Ana, Pide("SUSPENSION"));

        Assert.Equal("YA_SUSPENDIDO", (await Rechaza(e.Servicio.AplicarAsync(Admin, Ana, Pide("SUSPENSION")))).Codigo);
        Assert.Single(e.Repo.Sanciones);
    }

    [Fact]
    public async Task Todo_el_trabajo_de_una_sancion_ocurre_dentro_de_la_transaccion()
    {
        var e = Armar();

        await e.Servicio.AplicarAsync(Admin, Ana, Pide("ADVERTENCIA"));

        Assert.True(e.Repo.TodoDentroDeTransaccion);
        Assert.Equal(1, e.Tx.Ejecuciones);
    }

    // ---------- escalera y conteo

    [Fact]
    public async Task Tras_tres_advertencias_el_sistema_sugiere_suspension_pero_no_la_aplica()
    {
        var e = Armar();
        SancionAplicadaDto? ultima = null;
        for (var i = 0; i < 3; i++) ultima = await e.Servicio.AplicarAsync(Admin, Ana, Pide("ADVERTENCIA", $"Falta {i}"));

        Assert.Equal(3, ultima!.Advertencias);
        Assert.Equal("SUSPENSION", ultima.SiguienteSancionSugerida);
        Assert.Equal("ACTIVO", ultima.EstadoUsuario);
        Assert.All(e.Repo.Sanciones, s => Assert.Equal("ADVERTENCIA", s.Tipo));
    }

    [Fact]
    public async Task Una_advertencia_levantada_no_cuenta_para_las_tres()
    {
        var e = Armar();
        for (var i = 0; i < 3; i++) await e.Servicio.AplicarAsync(Admin, Ana, Pide("ADVERTENCIA", $"Falta {i}"));

        await e.Servicio.LevantarAsync(Admin, Ana, 2);
        var detalle = await e.Servicio.ObtenerAsync(Ana);

        Assert.Equal(2, detalle.Advertencias);
        Assert.Equal("ADVERTENCIA", detalle.SiguienteSancionSugerida);
        Assert.Equal(3, detalle.Sanciones.Count); // el historial conserva la levantada
    }

    // ---------- levantar

    [Fact]
    public async Task Levantar_registra_quien_y_cuando_y_la_suspension_deja_de_estar_vigente()
    {
        var e = Armar();
        await e.Servicio.AplicarAsync(Admin, Ana, Pide("SUSPENSION"));
        e.Validador.Invalidados.Clear();

        var levantada = await e.Servicio.LevantarAsync(Admin, Ana, 1);

        Assert.Equal(Ahora.UtcDateTime, levantada.LevantadaEn);
        Assert.Equal("Admin", levantada.LevantadaPor);
        Assert.False(levantada.Vigente);
        Assert.Equal("ACTIVO", (await e.Servicio.ObtenerAsync(Ana)).Estado);
        Assert.Equal([Ana], e.Validador.Invalidados);
    }

    [Fact]
    public async Task Levantar_dos_veces_la_misma_sancion_da_YA_LEVANTADA()
    {
        var e = Armar();
        await e.Servicio.AplicarAsync(Admin, Ana, Pide("ADVERTENCIA"));
        await e.Servicio.LevantarAsync(Admin, Ana, 1);

        Assert.Equal("YA_LEVANTADA", (await Rechaza(e.Servicio.LevantarAsync(Admin, Ana, 1))).Codigo);
    }

    [Fact]
    public async Task Una_sancion_de_otro_usuario_o_inexistente_da_NO_ENCONTRADO()
    {
        var e = Armar();
        await e.Servicio.AplicarAsync(Admin, Ana, Pide("ADVERTENCIA"));

        Assert.Equal("NO_ENCONTRADO", (await Rechaza(e.Servicio.LevantarAsync(Admin, 999, 1))).Codigo);
        Assert.Equal("NO_ENCONTRADO", (await Rechaza(e.Servicio.LevantarAsync(Admin, Ana, 50))).Codigo);
    }

    // ---------- baja

    [Fact]
    public async Task La_baja_purga_y_anonimiza_en_la_misma_transaccion_y_conserva_la_sancion()
    {
        var e = Armar();

        var respuesta = await e.Servicio.AplicarAsync(Admin, Ana, Pide("BAJA", "Fraude"));

        Assert.Equal(1, e.Tx.Ejecuciones);
        Assert.Equal(1, e.Purga.Anonimizaciones);
        Assert.Equal("BAJA", respuesta.EstadoUsuario);
        Assert.Null(e.Repo.Sanciones.Single().Hasta);
        Assert.Equal("Fraude", e.Repo.Sanciones.Single().Motivo);
        Assert.Equal([Ana], e.Validador.Invalidados);
    }

    [Fact]
    public async Task Una_cuenta_dada_de_baja_no_recibe_otra_sancion_ni_se_levanta_la_baja()
    {
        var e = Armar();
        await e.Servicio.AplicarAsync(Admin, Ana, Pide("BAJA"));

        Assert.Equal("YA_DADO_DE_BAJA", (await Rechaza(e.Servicio.AplicarAsync(Admin, Ana, Pide("ADVERTENCIA")))).Codigo);
        Assert.Equal("BAJA_DEFINITIVA", (await Rechaza(e.Servicio.LevantarAsync(Admin, Ana, 1))).Codigo);
        Assert.Equal(1, e.Purga.Anonimizaciones);
    }

    [Fact]
    public async Task Si_la_purga_falla_no_queda_sancion_guardada_porque_el_error_deshace_la_transaccion()
    {
        var e = Armar();
        // Repo cuya purga lanza: la excepción sube y el servicio no avisa ni invalida nada.
        var repoQueFalla = new PurgaQueFalla();
        var purga = new PurgaUsuarioService(repoQueFalla, e.Tx, e.Vivo, new Reloj());
        var servicio = new SancionService(e.Repo, purga, e.Tx, e.Vivo, e.Validador, new Reloj());

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.AplicarAsync(Admin, Ana, Pide("BAJA")));

        Assert.False(e.Tx.Abierta);
        Assert.Empty(e.Validador.Invalidados);
        Assert.Empty(e.Vivo.Avisados);
    }

    private sealed class PurgaQueFalla : IPurgaRepository
    {
        public Task<List<ReglasPurga.ComunidadCreada>> ListarComunidadesCreadasAsync(int usuarioId, CancellationToken ct = default) => throw new InvalidOperationException("falla");
        public Task CambiarCreadorAsync(int comunidadId, int nuevoCreadorId, DateTime ahoraUtc, CancellationToken ct = default) => throw new NotSupportedException();
        public Task EliminarComunidadAsync(int comunidadId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<(int ComunidadId, int Confirmados)>> EliminarParticipacionesAsync(int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task EliminarNotificacionesAsync(int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task EliminarEventosAsync(int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task EliminarReportesAsync(int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task EliminarRecomendacionesAsync(int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task EliminarLugaresRecuperadosAsync(int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task EliminarLogsAuditoriaAsync(int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task EliminarPuentesAsync(int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task EliminarHorariosAsync(int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task EliminarHistorialAsync(int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task EliminarSancionesAsync(int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AnonimizarUsuarioAsync(int usuarioId, string hashInutilizable, DateTime ahoraUtc, CancellationToken ct = default) => throw new NotSupportedException();
        public Task EliminarUsuarioAsync(int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    // ---------- lista y detalle

    [Fact]
    public async Task La_lista_trae_estado_y_cantidad_de_sanciones_con_busqueda()
    {
        var e = Armar();
        e.Repo.Usuarios[11] = new DatosUsuario(11, "Beto", "beto@ucb.edu.bo", "Derecho", 3, "ESTUDIANTE", true, new DateTime(2026, 3, 1));
        await e.Servicio.AplicarAsync(Admin, Ana, Pide("ADVERTENCIA"));
        await e.Servicio.AplicarAsync(Admin, Ana, Pide("SUSPENSION"));

        var todos = await e.Servicio.ListarAsync(null, null);
        var soloAna = await e.Servicio.ListarAsync("ana@", null);

        Assert.Equal(["Ana", "Beto"], todos.Select(u => u.Nombre).OrderBy(n => n));
        var ana = Assert.Single(soloAna);
        Assert.Equal(("SUSPENDIDO", 2, 1), (ana.Estado, ana.Sanciones, ana.Advertencias));
        Assert.DoesNotContain(todos, u => u.Nombre == "Admin");
    }

    [Fact]
    public async Task El_detalle_trae_el_historial_con_lo_mas_reciente_primero_y_el_bloqueo()
    {
        var e = Armar();
        await e.Servicio.AplicarAsync(Admin, Ana, Pide("ADVERTENCIA", "Primera"));
        await e.Servicio.AplicarAsync(Admin, Ana, Pide("SUSPENSION", "Segunda"));

        var detalle = await e.Servicio.ObtenerAsync(Ana);

        Assert.Equal(["Segunda", "Primera"], detalle.Sanciones.Select(s => s.Motivo));
        Assert.Equal("SUSPENSION", detalle.Bloqueo!.Tipo);
        Assert.Equal(3, detalle.AdvertenciasParaSuspension);
        Assert.Equal(7, detalle.DiasSuspensionPorDefecto);
    }

    [Fact]
    public async Task El_detalle_de_un_admin_o_de_alguien_que_no_existe_da_NO_ENCONTRADO()
    {
        var e = Armar();

        Assert.Equal("NO_ENCONTRADO", (await Rechaza(e.Servicio.ObtenerAsync(2))).Codigo);
        Assert.Equal("NO_ENCONTRADO", (await Rechaza(e.Servicio.ObtenerAsync(404))).Codigo);
    }
}

public class ValidadorSesionTests
{
    private static readonly DateTimeOffset Inicio = new(2026, 11, 13, 15, 0, 0, TimeSpan.Zero);

    private sealed class Reloj : TimeProvider
    {
        public DateTimeOffset Ahora { get; set; } = Inicio;
        public override DateTimeOffset GetUtcNow() => Ahora;
    }

    private sealed class Lector
    {
        public EstadoSesion? Estado { get; set; } = new(true, null);
        public int Lecturas { get; private set; }

        public Task<EstadoSesion?> Leer(int usuarioId, DateTime ahora, CancellationToken ct)
        {
            Lecturas++;
            return Task.FromResult(Estado);
        }
    }

    private static readonly ReglasSancion.BloqueoCuenta Suspension = new(Sancion.TipoSuspension, "Acoso", Inicio.UtcDateTime.AddDays(7));

    [Fact]
    public async Task Un_usuario_activo_y_sin_sancion_puede_usar_la_api()
    {
        var validador = new ValidadorSesion(new Lector().Leer, new Reloj());

        Assert.True(await validador.PuedeUsarApiAsync(10));
    }

    [Fact]
    public async Task Un_usuario_suspendido_o_dado_de_baja_no_puede_usar_la_api()
    {
        var lector = new Lector { Estado = new EstadoSesion(true, Suspension) };
        var validador = new ValidadorSesion(lector.Leer, new Reloj());
        Assert.False(await validador.PuedeUsarApiAsync(10));

        lector.Estado = new EstadoSesion(true, new ReglasSancion.BloqueoCuenta(Sancion.TipoBaja, "Fraude", null));
        validador.Invalidar(10);
        Assert.False(await validador.PuedeUsarApiAsync(10));
    }

    [Fact]
    public async Task Un_usuario_que_ya_no_existe_no_puede_usar_la_api()
        => Assert.False(await new ValidadorSesion(new Lector { Estado = null }.Leer, new Reloj()).PuedeUsarApiAsync(10));

    [Fact]
    public async Task El_resultado_se_guarda_60_segundos_y_despues_se_vuelve_a_leer()
    {
        var reloj = new Reloj();
        var lector = new Lector();
        var validador = new ValidadorSesion(lector.Leer, reloj);

        await validador.PuedeUsarApiAsync(10);
        reloj.Ahora = Inicio.AddSeconds(59);
        await validador.PuedeUsarApiAsync(10);
        Assert.Equal(1, lector.Lecturas);

        lector.Estado = new EstadoSesion(true, Suspension);
        Assert.True(await validador.PuedeUsarApiAsync(10)); // todavía dentro del caché: tarda hasta ~1 minuto en surtir efecto

        reloj.Ahora = Inicio.AddSeconds(61);
        Assert.False(await validador.PuedeUsarApiAsync(10));
        Assert.Equal(2, lector.Lecturas);
        Assert.Equal(TimeSpan.FromSeconds(60), ValidadorSesion.Vigencia);
    }

    [Fact]
    public async Task Invalidar_hace_que_la_sancion_surta_efecto_de_inmediato()
    {
        var lector = new Lector();
        var validador = new ValidadorSesion(lector.Leer, new Reloj());
        Assert.True(await validador.PuedeUsarApiAsync(10));

        lector.Estado = new EstadoSesion(true, Suspension);
        validador.Invalidar(10);

        Assert.False(await validador.PuedeUsarApiAsync(10));
    }

    [Fact]
    public async Task Cada_usuario_tiene_su_propio_resultado()
    {
        var lector = new Lector();
        var validador = new ValidadorSesion(lector.Leer, new Reloj());

        await validador.PuedeUsarApiAsync(10);
        await validador.PuedeUsarApiAsync(11);

        Assert.Equal(2, lector.Lecturas);
    }

    [Fact]
    public async Task Una_suspension_que_vence_deja_pasar_al_usuario_cuando_el_caché_se_renueva()
    {
        var reloj = new Reloj();
        var lector = new Lector { Estado = new EstadoSesion(true, Suspension) };
        var validador = new ValidadorSesion(lector.Leer, reloj);
        Assert.False(await validador.PuedeUsarApiAsync(10));

        lector.Estado = new EstadoSesion(true, null); // la fecha de fin llegó: la base ya no la devuelve como vigente
        reloj.Ahora = Inicio.AddDays(7).AddSeconds(5);

        Assert.True(await validador.PuedeUsarApiAsync(10));
    }
}
