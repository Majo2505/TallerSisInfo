using Breaku.Application.Common.Interfaces;
using Breaku.Application.Horarios;
using Breaku.Application.Perfil;
using Breaku.Application.Sanciones;
using Breaku.Application.TiempoReal;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Tests;

/// <summary>HU29: eliminar mi cuenta (reglas puras y orquestación con repositorios falsos).</summary>
public class ReglasEliminarCuentaTests
{
    private static readonly DateTime Ahora = new(2026, 11, 13, 15, 0, 0, DateTimeKind.Utc);

    private static ReglasSancion.Registro R(string tipo, DateTime? hasta = null, DateTime? levantada = null)
        => new(1, tipo, "motivo", Ahora.AddDays(-5), hasta, levantada);

    [Theory]
    [InlineData("ELIMINAR", true)]
    [InlineData("  ELIMINAR ", true)]
    [InlineData("eliminar", false)]
    [InlineData("ELIMINAR cuenta", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void La_confirmacion_es_exactamente_ELIMINAR(string? texto, bool valida)
        => Assert.Equal(valida, ReglasEliminarCuenta.ValidarConfirmacion(texto) is null);

    [Fact]
    public void El_admin_no_puede_eliminar_su_cuenta_y_el_estudiante_si()
    {
        Assert.NotNull(ReglasEliminarCuenta.ValidarRol("ADMIN"));
        Assert.Null(ReglasEliminarCuenta.ValidarRol("ESTUDIANTE"));
    }

    [Fact]
    public void Una_suspension_vigente_cuenta_como_sancion_activa()
        => Assert.True(ReglasEliminarCuenta.TieneSancionActiva([R("SUSPENSION", Ahora.AddDays(2))], Ahora));

    [Fact]
    public void Una_suspension_vencida_o_levantada_no_cuenta()
    {
        Assert.False(ReglasEliminarCuenta.TieneSancionActiva([R("SUSPENSION", Ahora.AddDays(-1))], Ahora));
        Assert.False(ReglasEliminarCuenta.TieneSancionActiva([R("SUSPENSION", Ahora.AddDays(2), Ahora.AddDays(-1))], Ahora));
    }

    [Fact]
    public void Una_advertencia_nunca_bloquea()
        => Assert.False(ReglasEliminarCuenta.TieneSancionActiva([R("ADVERTENCIA"), R("ADVERTENCIA")], Ahora));

    [Fact]
    public void Sin_sanciones_no_hay_sancion_activa()
        => Assert.False(ReglasEliminarCuenta.TieneSancionActiva([], Ahora));
}

public class EliminarCuentaServiceTests
{
    private const int Yo = 10;
    private const string GoogleSubMio = "g-yo";
    private static readonly DateTimeOffset Ahora = new(2026, 11, 13, 15, 0, 0, TimeSpan.Zero);

    private sealed class Reloj : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Ahora;
    }

    private sealed class Transaccion : ITransaccion
    {
        public int Ejecuciones { get; private set; }
        public bool Abierta { get; private set; }
        public bool HuboLlamadasFuera { get; set; }

        public async Task<T> EjecutarAsync<T>(Func<Task<T>> trabajo, CancellationToken ct = default)
        {
            if (Abierta) return await trabajo();
            Abierta = true;
            Ejecuciones++;
            try { return await trabajo(); }
            finally { Abierta = false; }
        }
    }

    private sealed class PerfilFalso(Usuario? usuario) : IPerfilRepository
    {
        public Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default) => Task.FromResult(usuario);
        public Task GuardarConHistorialAsync(int usuarioId, string a, string n, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class SancionesFalso(Transaccion tx, List<ReglasSancion.Registro> registros) : ISancionRepository
    {
        public bool Bloqueado { get; private set; }

        public Task BloquearUsuarioAsync(int usuarioId, CancellationToken ct = default)
        {
            if (!tx.Abierta) tx.HuboLlamadasFuera = true;
            Bloqueado = true;
            return Task.CompletedTask;
        }

        public Task<List<SancionDetalle>> ListarSancionesAsync(int usuarioId, CancellationToken ct = default)
        {
            if (!tx.Abierta) tx.HuboLlamadasFuera = true;
            return Task.FromResult(registros.Select(r => new SancionDetalle(r, null, "admin", null)).ToList());
        }

        public Task<List<DatosUsuario>> ListarEstudiantesAsync(string? buscar, int limite, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<RegistroDeUsuario>> ListarRegistrosAsync(IReadOnlyCollection<int> usuarioIds, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<DatosUsuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<SancionDetalle?> ObtenerSancionAsync(int usuarioId, int sancionId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> ReporteExisteAsync(int reporteId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> AgregarAsync(int usuarioId, ReglasSancion.SancionValida sancion, int? reporteId, int adminId, DateTime ahoraUtc, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> LevantarAsync(int sancionId, int adminId, DateTime ahoraUtc, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AgregarAvisoAdvertenciaAsync(int usuarioId, int sancionId, string mensaje, DateTime ahoraUtc, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<EstadoSesion?> ObtenerEstadoSesionAsync(int usuarioId, DateTime ahoraUtc, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class PurgaFalsa(Transaccion tx, List<ReglasPurga.ComunidadCreada> comunidades) : IPurgaRepository
    {
        public List<string> Llamadas { get; } = [];

        private Task Anotar(string llamada)
        {
            if (!tx.Abierta) tx.HuboLlamadasFuera = true;
            Llamadas.Add(llamada);
            return Task.CompletedTask;
        }

        public Task<List<ReglasPurga.ComunidadCreada>> ListarComunidadesCreadasAsync(int usuarioId, CancellationToken ct = default)
            => Anotar("ListarComunidades").ContinueWith(_ => comunidades, ct);
        public Task CambiarCreadorAsync(int comunidadId, int nuevoCreadorId, DateTime ahoraUtc, CancellationToken ct = default) => Anotar($"CambiarCreador:{comunidadId}->{nuevoCreadorId}");
        public Task EliminarComunidadAsync(int comunidadId, CancellationToken ct = default) => Anotar($"EliminarComunidad:{comunidadId}");
        public async Task<List<(int ComunidadId, int Confirmados)>> EliminarParticipacionesAsync(int usuarioId, CancellationToken ct = default)
        {
            await Anotar("Participaciones");
            return [];
        }
        public Task EliminarNotificacionesAsync(int usuarioId, CancellationToken ct = default) => Anotar("Notificaciones");
        public Task EliminarEventosAsync(int usuarioId, CancellationToken ct = default) => Anotar("Eventos");
        public Task EliminarReportesAsync(int usuarioId, CancellationToken ct = default) => Anotar("Reportes");
        public Task EliminarRecomendacionesAsync(int usuarioId, CancellationToken ct = default) => Anotar("Recomendaciones");
        public Task EliminarLugaresRecuperadosAsync(int usuarioId, CancellationToken ct = default) => Anotar("LugaresRecuperados");
        public Task EliminarLogsAuditoriaAsync(int usuarioId, CancellationToken ct = default) => Anotar("Logs");
        public Task EliminarPuentesAsync(int usuarioId, CancellationToken ct = default) => Anotar("Puentes");
        public Task EliminarHorariosAsync(int usuarioId, CancellationToken ct = default) => Anotar("Horarios");
        public Task EliminarHistorialAsync(int usuarioId, CancellationToken ct = default) => Anotar("Historial");
        public Task EliminarSancionesAsync(int usuarioId, CancellationToken ct = default) => Anotar("Sanciones");
        public Task AnonimizarUsuarioAsync(int usuarioId, string hashInutilizable, DateTime ahoraUtc, CancellationToken ct = default) => Anotar("Anonimizar");
        public Task EliminarUsuarioAsync(int usuarioId, CancellationToken ct = default) => Anotar("EliminarUsuario");
    }

    private sealed class Hasher : IHasherContrasenas
    {
        public int Verificaciones { get; private set; }
        public string Cifrar(string contrasena) => "hash:" + contrasena;
        public bool Verificar(string contrasena, string? hash)
        {
            Verificaciones++;
            return hash is not null && hash == "hash:" + contrasena;
        }
    }

    private sealed class GoogleFalso(ReglasCuenta.IdentidadGoogle? identidad, bool configurado = true) : IVerificadorGoogle
    {
        public int Llamadas { get; private set; }
        public bool Configurado => configurado;
        public Task<ReglasCuenta.IdentidadGoogle?> VerificarAsync(string credencial, CancellationToken ct = default)
        {
            Llamadas++;
            return Task.FromResult(identidad);
        }
    }

    private sealed class ValidadorFalso : IValidadorSesion
    {
        public List<int> Invalidados { get; } = [];
        public Task<ResultadoSesion> ValidarAsync(int usuarioId, CancellationToken ct = default) => Task.FromResult(new ResultadoSesion(true));
        public void Invalidar(int usuarioId) => Invalidados.Add(usuarioId);
    }

    private sealed class TiempoRealFalso(bool falla = false) : ITiempoReal
    {
        public List<int> Avisados { get; } = [];
        public Task AvisosCambiaronAsync(IReadOnlyCollection<int> usuarioIds, CancellationToken ct = default)
        {
            if (falla) throw new InvalidOperationException("sin canal");
            Avisados.AddRange(usuarioIds);
            return Task.CompletedTask;
        }
        public Task ComunidadCreadaAsync(int comunidadId, CancellationToken ct = default) => Task.CompletedTask;
        public Task AvisosDeComunidadAsync(int comunidadId, CancellationToken ct = default) => Task.CompletedTask;
        public Task ComunidadActualizadaAsync(int comunidadId, string estado, int confirmados, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed record Contexto(EliminarCuentaService Servicio, PurgaFalsa Purga, SancionesFalso Sanciones, Transaccion Tx,
        Hasher Hasher, GoogleFalso Google, ValidadorFalso Validador, TiempoRealFalso Vivo);

    private static Usuario Estudiante(string? hash = "hash:secreta1", string? googleSub = null, string rol = "ESTUDIANTE")
        => new() { Id = Yo, Email = "yo@ucb.edu.bo", Nombre = "Yo", Rol = rol, PasswordHash = hash, GoogleSub = googleSub, Activo = true };

    private static Contexto Armar(Usuario? usuario = null, List<ReglasSancion.Registro>? sanciones = null,
        List<ReglasPurga.ComunidadCreada>? comunidades = null, ReglasCuenta.IdentidadGoogle? identidad = null,
        bool googleConfigurado = true, bool avisoFalla = false)
    {
        var tx = new Transaccion();
        var purgaRepo = new PurgaFalsa(tx, comunidades ?? []);
        var sancionesRepo = new SancionesFalso(tx, sanciones ?? []);
        var vivo = new TiempoRealFalso(avisoFalla);
        var purga = new PurgaUsuarioService(purgaRepo, tx, vivo, new Reloj());
        var hasher = new Hasher();
        var google = new GoogleFalso(identidad, googleConfigurado);
        var validador = new ValidadorFalso();
        var servicio = new EliminarCuentaService(new PerfilFalso(usuario ?? Estudiante()), sancionesRepo, purga, tx, hasher, google, validador, new Reloj());
        return new Contexto(servicio, purgaRepo, sancionesRepo, tx, hasher, google, validador, vivo);
    }

    private static EliminarCuentaRequest ConContrasena(string contrasena = "secreta1", string confirmacion = "ELIMINAR")
        => new(confirmacion, contrasena, null);

    private static async Task<ReglaNegocioException> Rechazo(Func<Task> accion) => await Assert.ThrowsAsync<ReglaNegocioException>(accion);

    private static ReglasPurga.ParticipanteComunidad P(int usuario, string estado, int? minutos = null)
        => new(usuario, estado, minutos is null ? null : Ahora.UtcDateTime.AddMinutes(minutos.Value));

    // ---------- el camino feliz

    [Fact]
    public async Task Con_la_contrasena_correcta_purga_con_eliminarCuenta_y_borra_la_fila_al_final()
    {
        var c = Armar();

        await c.Servicio.EliminarAsync(Yo, ConContrasena());

        Assert.Equal("EliminarUsuario", c.Purga.Llamadas[^1]);
        Assert.DoesNotContain("Anonimizar", c.Purga.Llamadas);
        Assert.Contains("Sanciones", c.Purga.Llamadas);
        Assert.True(c.Purga.Llamadas.IndexOf("Notificaciones") < c.Purga.Llamadas.IndexOf("Sanciones"));
        Assert.True(c.Purga.Llamadas.IndexOf("Eventos") < c.Purga.Llamadas.IndexOf("EliminarUsuario"));
    }

    [Fact]
    public async Task Todo_corre_en_una_sola_transaccion_con_la_fila_bloqueada()
    {
        var c = Armar();

        await c.Servicio.EliminarAsync(Yo, ConContrasena());

        Assert.Equal(1, c.Tx.Ejecuciones);
        Assert.False(c.Tx.HuboLlamadasFuera);
        Assert.True(c.Sanciones.Bloqueado);
    }

    [Fact]
    public async Task Despues_de_eliminar_el_cache_de_la_sesion_se_invalida()
    {
        var c = Armar();

        await c.Servicio.EliminarAsync(Yo, ConContrasena());

        Assert.Equal([Yo], c.Validador.Invalidados);
    }

    [Fact]
    public async Task Si_falla_el_aviso_en_vivo_el_borrado_igual_responde_bien()
    {
        var c = Armar(comunidades: [new(1, "ACTIVA", [P(Yo, "CONFIRMADO", 0), P(21, "CONFIRMADO", 5)])], avisoFalla: true);

        await c.Servicio.EliminarAsync(Yo, ConContrasena());

        Assert.Equal("EliminarUsuario", c.Purga.Llamadas[^1]);
        Assert.Equal([Yo], c.Validador.Invalidados);
    }

    // ---------- reautenticación

    [Fact]
    public async Task Con_la_contrasena_incorrecta_se_rechaza_sin_borrar_nada()
    {
        var c = Armar();

        var ex = await Rechazo(() => c.Servicio.EliminarAsync(Yo, ConContrasena("otra-cosa1")));

        Assert.Equal(ReglasEliminarCuenta.ErrorContrasenaIncorrecta, ex.Codigo);
        Assert.Empty(c.Purga.Llamadas);
        Assert.Equal(0, c.Tx.Ejecuciones);
        Assert.Empty(c.Validador.Invalidados);
    }

    [Fact]
    public async Task Sin_contrasena_ni_google_se_rechaza()
    {
        var c = Armar();

        var ex = await Rechazo(() => c.Servicio.EliminarAsync(Yo, new("ELIMINAR", null, null)));

        Assert.Equal("VALIDACION", ex.Codigo);
        Assert.Empty(c.Purga.Llamadas);
    }

    [Fact]
    public async Task Una_cuenta_solo_de_Google_se_reautentica_con_un_login_de_Google_del_mismo_sub()
    {
        var c = Armar(Estudiante(hash: null, googleSub: GoogleSubMio), identidad: new(GoogleSubMio, "yo@ucb.edu.bo", true, "Yo"));

        await c.Servicio.EliminarAsync(Yo, new("ELIMINAR", null, "token-google"));

        Assert.Equal(1, c.Google.Llamadas);
        Assert.Equal("EliminarUsuario", c.Purga.Llamadas[^1]);
    }

    [Fact]
    public async Task Un_login_de_Google_de_otra_cuenta_se_rechaza()
    {
        var c = Armar(Estudiante(hash: null, googleSub: GoogleSubMio), identidad: new("g-otro", "otro@ucb.edu.bo", true, "Otro"));

        var ex = await Rechazo(() => c.Servicio.EliminarAsync(Yo, new("ELIMINAR", null, "token-google")));

        Assert.Equal(ReglasEliminarCuenta.ErrorGoogleIncorrecto, ex.Codigo);
        Assert.Empty(c.Purga.Llamadas);
    }

    [Fact]
    public async Task Un_token_de_Google_invalido_o_con_correo_sin_verificar_se_rechaza()
    {
        var invalido = Armar(Estudiante(hash: null, googleSub: GoogleSubMio), identidad: null);
        var sinVerificar = Armar(Estudiante(hash: null, googleSub: GoogleSubMio), identidad: new(GoogleSubMio, "yo@ucb.edu.bo", false, "Yo"));

        await Rechazo(() => invalido.Servicio.EliminarAsync(Yo, new("ELIMINAR", null, "x")));
        await Rechazo(() => sinVerificar.Servicio.EliminarAsync(Yo, new("ELIMINAR", null, "x")));

        Assert.Empty(invalido.Purga.Llamadas);
        Assert.Empty(sinVerificar.Purga.Llamadas);
    }

    [Fact]
    public async Task Una_cuenta_sin_Google_enlazado_no_se_reautentica_con_Google()
    {
        var c = Armar(Estudiante(googleSub: null), identidad: new("g-cualquiera", "yo@ucb.edu.bo", true, "Yo"));

        var ex = await Rechazo(() => c.Servicio.EliminarAsync(Yo, new("ELIMINAR", null, "token-google")));

        Assert.Equal(ReglasEliminarCuenta.ErrorGoogleIncorrecto, ex.Codigo);
        Assert.Equal(0, c.Google.Llamadas);
    }

    [Fact]
    public async Task Una_cuenta_con_contrasena_y_Google_acepta_cualquiera_de_las_dos()
    {
        var conPass = Armar(Estudiante(googleSub: GoogleSubMio));
        var conGoogle = Armar(Estudiante(googleSub: GoogleSubMio), identidad: new(GoogleSubMio, "yo@ucb.edu.bo", true, "Yo"));

        await conPass.Servicio.EliminarAsync(Yo, ConContrasena());
        await conGoogle.Servicio.EliminarAsync(Yo, new("ELIMINAR", null, "token-google"));

        Assert.Equal("EliminarUsuario", conPass.Purga.Llamadas[^1]);
        Assert.Equal(0, conPass.Google.Llamadas);
        Assert.Equal("EliminarUsuario", conGoogle.Purga.Llamadas[^1]);
    }

    [Fact]
    public async Task Con_contrasena_mala_y_Google_bueno_se_acepta_Google()
    {
        var c = Armar(Estudiante(googleSub: GoogleSubMio), identidad: new(GoogleSubMio, "yo@ucb.edu.bo", true, "Yo"));

        await c.Servicio.EliminarAsync(Yo, new("ELIMINAR", "mala-clave1", "token-google"));

        Assert.Equal("EliminarUsuario", c.Purga.Llamadas[^1]);
    }

    [Fact]
    public async Task Una_cuenta_solo_de_Google_no_se_reautentica_con_una_contrasena()
    {
        var c = Armar(Estudiante(hash: null, googleSub: GoogleSubMio));

        var ex = await Rechazo(() => c.Servicio.EliminarAsync(Yo, ConContrasena("cualquiera1")));

        Assert.Equal(ReglasEliminarCuenta.ErrorContrasenaIncorrecta, ex.Codigo);
        Assert.Empty(c.Purga.Llamadas);
    }

    // ---------- confirmación, rol y sanciones

    [Theory]
    [InlineData("eliminar")]
    [InlineData("")]
    [InlineData("SI")]
    public async Task Sin_el_texto_de_confirmacion_no_se_borra_ni_se_verifica_la_contrasena(string texto)
    {
        var c = Armar();

        var ex = await Rechazo(() => c.Servicio.EliminarAsync(Yo, ConContrasena(confirmacion: texto)));

        Assert.Equal("VALIDACION", ex.Codigo);
        Assert.Equal(0, c.Hasher.Verificaciones);
        Assert.Empty(c.Purga.Llamadas);
    }

    [Fact]
    public async Task El_admin_no_puede_eliminar_su_cuenta()
    {
        var c = Armar(Estudiante(rol: "ADMIN"));

        var ex = await Rechazo(() => c.Servicio.EliminarAsync(Yo, ConContrasena()));

        Assert.Equal(ReglasEliminarCuenta.ErrorNoEliminable, ex.Codigo);
        Assert.Empty(c.Purga.Llamadas);
    }

    [Fact]
    public async Task Con_una_suspension_vigente_se_rechaza_con_el_mensaje_del_issue_y_no_se_borra_nada()
    {
        var c = Armar(sanciones: [new(1, "SUSPENSION", "motivo", Ahora.UtcDateTime.AddDays(-1), Ahora.UtcDateTime.AddDays(3), null)]);

        var ex = await Rechazo(() => c.Servicio.EliminarAsync(Yo, ConContrasena()));

        Assert.Equal(ReglasEliminarCuenta.ErrorSancionActiva, ex.Codigo);
        Assert.Equal("Tienes una sanción activa; contacta al administrador", ex.Message);
        Assert.Empty(c.Purga.Llamadas);
        Assert.Empty(c.Validador.Invalidados);
    }

    [Fact]
    public async Task Advertencias_y_suspensiones_vencidas_no_impiden_eliminar()
    {
        var c = Armar(sanciones:
        [
            new(1, "ADVERTENCIA", "a", Ahora.UtcDateTime.AddDays(-9), null, null),
            new(2, "ADVERTENCIA", "b", Ahora.UtcDateTime.AddDays(-8), null, null),
            new(3, "SUSPENSION", "c", Ahora.UtcDateTime.AddDays(-7), Ahora.UtcDateTime.AddDays(-1), null)
        ]);

        await c.Servicio.EliminarAsync(Yo, ConContrasena());

        Assert.Equal("EliminarUsuario", c.Purga.Llamadas[^1]);
    }

    [Fact]
    public async Task Si_la_cuenta_no_existe_responde_no_encontrado()
    {
        var tx = new Transaccion();
        var vivo = new TiempoRealFalso();
        var servicio = new EliminarCuentaService(new PerfilFalso(null), new SancionesFalso(tx, []),
            new PurgaUsuarioService(new PurgaFalsa(tx, []), tx, vivo, new Reloj()), tx, new Hasher(), new GoogleFalso(null),
            new ValidadorFalso(), new Reloj());

        var ex = await Rechazo(() => servicio.EliminarAsync(Yo, ConContrasena()));

        Assert.Equal("NO_ENCONTRADO", ex.Codigo);
    }

    // ---------- comunidades y eventos (orden de borrado)

    [Fact]
    public async Task Comunidad_activa_con_otros_confirmados_cambia_de_creador_antes_de_borrar_los_logs()
    {
        var c = Armar(comunidades: [new(7, "ACTIVA", [P(Yo, "CONFIRMADO", 0), P(21, "CONFIRMADO", 30), P(22, "CONFIRMADO", 5)])]);

        await c.Servicio.EliminarAsync(Yo, ConContrasena());

        var llamadas = c.Purga.Llamadas;
        Assert.Contains("CambiarCreador:7->22", llamadas);
        Assert.DoesNotContain("EliminarComunidad:7", llamadas);
        Assert.True(llamadas.IndexOf("CambiarCreador:7->22") < llamadas.IndexOf("Logs"));
        Assert.Equal("EliminarUsuario", llamadas[^1]);
    }

    [Fact]
    public async Task Comunidad_con_solo_pendientes_o_rechazados_se_elimina()
    {
        var c = Armar(comunidades: [new(8, "ACTIVA", [P(Yo, "CONFIRMADO", 0), P(21, "PENDIENTE"), P(22, "RECHAZADO", 1)])]);

        await c.Servicio.EliminarAsync(Yo, ConContrasena());

        Assert.Contains("EliminarComunidad:8", c.Purga.Llamadas);
        Assert.DoesNotContain(c.Purga.Llamadas, l => l.StartsWith("CambiarCreador"));
    }

    [Fact]
    public async Task Comunidad_archivada_se_elimina_aunque_tenga_confirmados()
    {
        var c = Armar(comunidades: [new(9, "ARCHIVADA", [P(Yo, "CONFIRMADO", 0), P(21, "CONFIRMADO", 5)])]);

        await c.Servicio.EliminarAsync(Yo, ConContrasena());

        Assert.Contains("EliminarComunidad:9", c.Purga.Llamadas);
        Assert.DoesNotContain(c.Purga.Llamadas, l => l.StartsWith("CambiarCreador"));
    }

    [Fact]
    public async Task Los_eventos_se_borran_despues_de_las_notificaciones_y_antes_de_la_fila_de_usuario()
    {
        var c = Armar();

        await c.Servicio.EliminarAsync(Yo, ConContrasena());

        var l = c.Purga.Llamadas;
        Assert.True(l.IndexOf("Notificaciones") < l.IndexOf("Eventos"));
        Assert.True(l.IndexOf("Eventos") < l.IndexOf("EliminarUsuario"));
    }

    [Fact]
    public async Task Los_participantes_de_otras_comunidades_afectadas_se_avisan_sin_incluir_al_propio_usuario()
    {
        var c = Armar(comunidades: [new(7, "ACTIVA", [P(Yo, "CONFIRMADO", 0), P(21, "CONFIRMADO", 5), P(22, "PENDIENTE")])]);

        await c.Servicio.EliminarAsync(Yo, ConContrasena());

        Assert.DoesNotContain(Yo, c.Vivo.Avisados);
        Assert.Contains(21, c.Vivo.Avisados);
        Assert.Contains(22, c.Vivo.Avisados);
    }
}
