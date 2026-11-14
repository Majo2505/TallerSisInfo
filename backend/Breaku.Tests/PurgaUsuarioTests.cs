using Breaku.Application.Sanciones;
using Breaku.Application.TiempoReal;
using Breaku.Domain;
using Breaku.Domain.Rules;
using static Breaku.Domain.Rules.ReglasPurga;

namespace Breaku.Tests;

public class ReglasPurgaTests
{
    private const int Yo = 10;
    private static readonly DateTime T0 = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

    private static ParticipanteComunidad P(int usuario, string estado, int? minutos = null)
        => new(usuario, estado, minutos is null ? null : T0.AddMinutes(minutos.Value));

    private static ComunidadCreada Activa(params ParticipanteComunidad[] participantes) => new(1, ComunidadEfimera.EstadoActiva, participantes);

    [Fact]
    public void Con_otros_participantes_confirmados_la_comunidad_sigue_abierta_con_el_de_menor_respondido_en()
    {
        var decision = DecidirComunidad(Yo, Activa(P(Yo, "CONFIRMADO", 0), P(21, "CONFIRMADO", 30), P(22, "CONFIRMADO", 5), P(23, "CONFIRMADO", 60)));

        Assert.Equal(Destino.CambiarCreador, decision.Destino);
        Assert.Equal(22, decision.NuevoCreadorId);
        Assert.Equal(3, decision.ConfirmadosRestantes);
    }

    [Fact]
    public void Si_empatan_en_respondido_en_gana_el_menor_id()
    {
        var decision = DecidirComunidad(Yo, Activa(P(Yo, "CONFIRMADO", 0), P(40, "CONFIRMADO", 5), P(30, "CONFIRMADO", 5)));

        Assert.Equal(30, decision.NuevoCreadorId);
    }

    [Fact]
    public void Solo_los_confirmados_cuentan_como_otros_participantes_el_pendiente_y_el_rechazado_no_heredan()
    {
        var decision = DecidirComunidad(Yo, Activa(P(Yo, "CONFIRMADO", 0), P(21, "PENDIENTE"), P(22, "RECHAZADO", 1), P(23, "CONFIRMADO", 90)));

        Assert.Equal(23, decision.NuevoCreadorId);
        Assert.Equal(1, decision.ConfirmadosRestantes);
    }

    [Fact]
    public void Si_solo_hay_pendientes_o_rechazados_la_comunidad_se_elimina()
    {
        var decision = DecidirComunidad(Yo, Activa(P(Yo, "CONFIRMADO", 0), P(21, "PENDIENTE"), P(22, "RECHAZADO", 1)));

        Assert.Equal(Destino.Eliminar, decision.Destino);
        Assert.Null(decision.NuevoCreadorId);
    }

    [Fact]
    public void Si_el_usuario_estaba_solo_la_comunidad_se_elimina()
        => Assert.Equal(Destino.Eliminar, DecidirComunidad(Yo, Activa(P(Yo, "CONFIRMADO", 0))).Destino);

    [Fact]
    public void El_propio_usuario_nunca_hereda_su_comunidad()
        => Assert.Equal(Destino.Eliminar, DecidirComunidad(Yo, Activa(P(Yo, "CONFIRMADO", -100))).Destino);

    [Fact]
    public void Una_comunidad_archivada_se_elimina_aunque_tenga_confirmados()
    {
        var archivada = new ComunidadCreada(1, ComunidadEfimera.EstadoArchivada, [P(Yo, "CONFIRMADO", 0), P(21, "CONFIRMADO", 5)]);

        Assert.Equal(Destino.Eliminar, DecidirComunidad(Yo, archivada).Destino);
    }

    // ---------- el orden de los pasos

    [Fact]
    public void La_baja_conserva_las_sanciones_y_la_eliminacion_de_cuenta_las_borra()
    {
        Assert.DoesNotContain(Paso.Sanciones, Pasos(eliminarCuenta: false));
        Assert.Contains(Paso.Sanciones, Pasos(eliminarCuenta: true));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cada_paso_va_antes_que_el_que_lo_referencia_por_clave_foranea(bool eliminarCuenta)
    {
        var pasos = Pasos(eliminarCuenta).ToList();
        int En(Paso p) => pasos.IndexOf(p);

        // Las comunidades (con su log_id) antes que los logs; las notificaciones antes que lo que apuntan.
        Assert.True(En(Paso.ComunidadesCreadas) < En(Paso.LogsAuditoria), "log_id en NULL antes de borrar los logs");
        Assert.True(En(Paso.ComunidadesCreadas) < En(Paso.Participaciones));
        Assert.True(En(Paso.Notificaciones) < En(Paso.Eventos), "notificacion.evento_id → evento");
        Assert.True(En(Paso.Notificaciones) < En(Paso.Conexiones), "sus avisos de conexión se van con sus notificaciones");
        Assert.True(En(Paso.Conexiones) < En(Paso.Usuario), "conexion y test_afinidad → usuario");
        Assert.True(En(Paso.Notificaciones) < En(Paso.Reportes), "notificacion.reporte_id → reporte");
        Assert.True(En(Paso.Eventos) < En(Paso.Usuario));
        Assert.True(En(Paso.Recomendaciones) < En(Paso.LugaresRecuperados), "recomendacion → lugar_recuperado");
        Assert.True(En(Paso.LugaresRecuperados) < En(Paso.LogsAuditoria), "lugar_recuperado → log_auditoria_rag");
        Assert.True(En(Paso.LogsAuditoria) < En(Paso.Usuario));
        Assert.True(En(Paso.Puentes) < En(Paso.Usuario) && En(Paso.Horarios) < En(Paso.Usuario) && En(Paso.HistorialCambio) < En(Paso.Usuario));
        Assert.Equal(Paso.Usuario, pasos[^1]);
        if (eliminarCuenta) Assert.True(En(Paso.Notificaciones) < En(Paso.Sanciones), "notificacion.sancion_id → sancion");
    }
}

public class PurgaUsuarioServiceTests
{
    private const int Yo = 10;
    private static readonly DateTimeOffset Ahora = new(2026, 11, 13, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTime T0 = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

    private sealed class Reloj : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Ahora;
    }

    private sealed class Transaccion : ITransaccion
    {
        public int Ejecuciones { get; private set; }
        public bool Abierta { get; private set; }
        public bool HuboLlamadasFueraDeLaTransaccion { get; set; }

        public async Task<T> EjecutarAsync<T>(Func<Task<T>> trabajo, CancellationToken ct = default)
        {
            if (Abierta) return await trabajo();
            Abierta = true;
            Ejecuciones++;
            try { return await trabajo(); }
            finally { Abierta = false; }
        }
    }

    private sealed class RepoFalso(Transaccion tx, List<ComunidadCreada> comunidades) : IPurgaRepository
    {
        public List<string> Llamadas { get; } = [];
        public List<(int Comunidad, int Nuevo, DateTime Ahora)> CambiosDeCreador { get; } = [];
        public List<int> Eliminadas { get; } = [];
        public string? HashRecibido { get; private set; }

        private Task Anotar(string llamada)
        {
            if (!tx.Abierta) tx.HuboLlamadasFueraDeLaTransaccion = true;
            Llamadas.Add(llamada);
            return Task.CompletedTask;
        }

        public Task<List<ComunidadCreada>> ListarComunidadesCreadasAsync(int usuarioId, CancellationToken ct = default)
            => Anotar("ListarComunidades").ContinueWith(_ => comunidades, ct);

        public async Task CambiarCreadorAsync(int comunidadId, int nuevoCreadorId, DateTime ahoraUtc, CancellationToken ct = default)
        {
            await Anotar($"CambiarCreador:{comunidadId}");
            CambiosDeCreador.Add((comunidadId, nuevoCreadorId, ahoraUtc));
        }

        public async Task EliminarComunidadAsync(int comunidadId, CancellationToken ct = default)
        {
            await Anotar($"EliminarComunidad:{comunidadId}");
            Eliminadas.Add(comunidadId);
        }

        public async Task<List<(int ComunidadId, int Confirmados)>> EliminarParticipacionesAsync(int usuarioId, CancellationToken ct = default)
        {
            await Anotar("Participaciones");
            return [(99, 2)];
        }

        public Task EliminarNotificacionesAsync(int usuarioId, CancellationToken ct = default) => Anotar("Notificaciones");
        public Task EliminarConexionesAsync(int usuarioId, CancellationToken ct = default) => Anotar("Conexiones");
        public Task EliminarEventosAsync(int usuarioId, CancellationToken ct = default) => Anotar("Eventos");
        public Task EliminarReportesAsync(int usuarioId, CancellationToken ct = default) => Anotar("Reportes");
        public Task EliminarRecomendacionesAsync(int usuarioId, CancellationToken ct = default) => Anotar("Recomendaciones");
        public Task EliminarLugaresRecuperadosAsync(int usuarioId, CancellationToken ct = default) => Anotar("LugaresRecuperados");
        public Task EliminarLogsAuditoriaAsync(int usuarioId, CancellationToken ct = default) => Anotar("Logs");
        public Task EliminarPuentesAsync(int usuarioId, CancellationToken ct = default) => Anotar("Puentes");
        public Task EliminarHorariosAsync(int usuarioId, CancellationToken ct = default) => Anotar("Horarios");
        public Task EliminarHistorialAsync(int usuarioId, CancellationToken ct = default) => Anotar("Historial");
        public Task EliminarSancionesAsync(int usuarioId, CancellationToken ct = default) => Anotar("Sanciones");

        public async Task AnonimizarUsuarioAsync(int usuarioId, string hashInutilizable, DateTime ahoraUtc, CancellationToken ct = default)
        {
            HashRecibido = hashInutilizable;
            await Anotar("Anonimizar");
        }

        public Task EliminarUsuarioAsync(int usuarioId, CancellationToken ct = default) => Anotar("EliminarUsuario");
    }

    private sealed class TiempoRealFalso : ITiempoReal
    {
        public List<int> Avisados { get; } = [];
        public List<(int Comunidad, string Estado, int Confirmados)> Actualizadas { get; } = [];

        public Task AvisosCambiaronAsync(IReadOnlyCollection<int> usuarioIds, CancellationToken ct = default)
        {
            Avisados.AddRange(usuarioIds);
            return Task.CompletedTask;
        }

        public Task ComunidadCreadaAsync(int comunidadId, CancellationToken ct = default) => Task.CompletedTask;
        public Task AvisosDeComunidadAsync(int comunidadId, CancellationToken ct = default) => Task.CompletedTask;

        public Task ComunidadActualizadaAsync(int comunidadId, string estado, int confirmados, CancellationToken ct = default)
        {
            Actualizadas.Add((comunidadId, estado, confirmados));
            return Task.CompletedTask;
        }
    }

    private static (PurgaUsuarioService Servicio, RepoFalso Repo, Transaccion Tx, TiempoRealFalso Vivo) Armar(params ComunidadCreada[] comunidades)
    {
        var tx = new Transaccion();
        var repo = new RepoFalso(tx, comunidades.ToList());
        var vivo = new TiempoRealFalso();
        return (new PurgaUsuarioService(repo, tx, vivo, new Reloj()), repo, tx, vivo);
    }

    private static ParticipanteComunidad P(int usuario, string estado, int? minutos = null)
        => new(usuario, estado, minutos is null ? null : T0.AddMinutes(minutos.Value));

    private static List<string> Pasos(RepoFalso repo, params string[] excluir)
        => repo.Llamadas.Where(l => !excluir.Contains(l) && !l.StartsWith("ListarComunidades")).ToList();

    [Fact]
    public async Task La_baja_ejecuta_los_pasos_en_el_orden_de_las_claves_foraneas_y_anonimiza_la_fila()
    {
        var (servicio, repo, _, _) = Armar();

        await servicio.PurgarAsync(Yo, eliminarCuenta: false);

        Assert.Equal(
            ["Participaciones", "Notificaciones", "Conexiones", "Eventos", "Reportes", "Recomendaciones", "LugaresRecuperados", "Logs",
             "Puentes", "Horarios", "Historial", "Anonimizar"],
            Pasos(repo));
        Assert.DoesNotContain("Sanciones", repo.Llamadas);
        Assert.DoesNotContain("EliminarUsuario", repo.Llamadas);
    }

    [Fact]
    public async Task La_eliminacion_de_cuenta_borra_las_sanciones_despues_de_las_notificaciones_y_luego_la_fila()
    {
        var (servicio, repo, _, _) = Armar();

        await servicio.PurgarAsync(Yo, eliminarCuenta: true);

        var llamadas = Pasos(repo);
        Assert.True(llamadas.IndexOf("Notificaciones") < llamadas.IndexOf("Sanciones"));
        Assert.True(llamadas.IndexOf("Sanciones") < llamadas.IndexOf("EliminarUsuario"));
        Assert.Equal("EliminarUsuario", llamadas[^1]);
        Assert.DoesNotContain("Anonimizar", llamadas);
    }

    [Fact]
    public async Task Todo_corre_en_una_sola_transaccion()
    {
        var (servicio, _, tx, _) = Armar(new ComunidadCreada(1, "ACTIVA", [P(Yo, "CONFIRMADO", 0), P(21, "CONFIRMADO", 5)]));

        await servicio.PurgarAsync(Yo, eliminarCuenta: false);

        Assert.Equal(1, tx.Ejecuciones);
        Assert.False(tx.HuboLlamadasFueraDeLaTransaccion);
    }

    [Fact]
    public async Task Dentro_de_otra_transaccion_la_purga_se_une_a_ella()
    {
        var (servicio, _, tx, _) = Armar();

        await tx.EjecutarAsync(async () =>
        {
            await servicio.PurgarAsync(Yo, eliminarCuenta: false);
            return 0;
        });

        Assert.Equal(1, tx.Ejecuciones);
    }

    [Fact]
    public async Task La_fila_anonimizada_recibe_un_hash_inutilizable_y_la_hora_del_reloj()
    {
        var (servicio, repo, _, _) = Armar();

        await servicio.PurgarAsync(Yo, eliminarCuenta: false);

        Assert.StartsWith("!baja!", repo.HashRecibido);
    }

    [Fact]
    public async Task Una_comunidad_que_sobrevive_cambia_de_creador_antes_de_borrar_los_logs()
    {
        var (servicio, repo, _, _) = Armar(new ComunidadCreada(7, "ACTIVA", [P(Yo, "CONFIRMADO", 0), P(21, "CONFIRMADO", 30), P(22, "CONFIRMADO", 5)]));

        await servicio.PurgarAsync(Yo, eliminarCuenta: false);

        var cambio = Assert.Single(repo.CambiosDeCreador);
        Assert.Equal((7, 22, Ahora.UtcDateTime), cambio);
        Assert.Empty(repo.Eliminadas);
        Assert.True(repo.Llamadas.IndexOf("CambiarCreador:7") < repo.Llamadas.IndexOf("Logs"), "log_id en NULL antes de borrar los logs");
        Assert.True(repo.Llamadas.IndexOf("CambiarCreador:7") < repo.Llamadas.IndexOf("Participaciones"));
    }

    [Fact]
    public async Task Una_comunidad_con_solo_pendientes_o_rechazados_se_elimina_sin_cambiar_de_creador()
    {
        var (servicio, repo, _, _) = Armar(new ComunidadCreada(8, "ACTIVA", [P(Yo, "CONFIRMADO", 0), P(21, "PENDIENTE"), P(22, "RECHAZADO", 4)]));

        await servicio.PurgarAsync(Yo, eliminarCuenta: false);

        Assert.Equal([8], repo.Eliminadas);
        Assert.Empty(repo.CambiosDeCreador);
    }

    [Fact]
    public async Task Una_comunidad_archivada_se_elimina()
    {
        var (servicio, repo, _, _) = Armar(new ComunidadCreada(9, "ARCHIVADA", [P(Yo, "CONFIRMADO", 0), P(21, "CONFIRMADO", 4)]));

        await servicio.PurgarAsync(Yo, eliminarCuenta: false);

        Assert.Equal([9], repo.Eliminadas);
        Assert.Empty(repo.CambiosDeCreador);
    }

    [Fact]
    public async Task Cada_comunidad_se_decide_por_separado()
    {
        var (servicio, repo, _, _) = Armar(
            new ComunidadCreada(1, "ACTIVA", [P(Yo, "CONFIRMADO", 0), P(21, "CONFIRMADO", 4)]),
            new ComunidadCreada(2, "ACTIVA", [P(Yo, "CONFIRMADO", 0)]),
            new ComunidadCreada(3, "ARCHIVADA", [P(Yo, "CONFIRMADO", 0)]));

        await servicio.PurgarAsync(Yo, eliminarCuenta: false);

        Assert.Equal([1], repo.CambiosDeCreador.Select(c => c.Comunidad));
        Assert.Equal([2, 3], repo.Eliminadas);
    }

    [Fact]
    public async Task Se_avisa_en_vivo_al_nuevo_organizador_a_los_demas_y_el_cupo_de_las_comunidades_activas()
    {
        var (servicio, _, _, vivo) = Armar(
            new ComunidadCreada(7, "ACTIVA", [P(Yo, "CONFIRMADO", 0), P(21, "CONFIRMADO", 5), P(22, "CONFIRMADO", 9), P(23, "PENDIENTE")]));

        var resultado = await servicio.PurgarAsync(Yo, eliminarCuenta: false);
        await servicio.AvisarAsync(resultado);

        Assert.DoesNotContain(Yo, vivo.Avisados);
        Assert.Equal([21, 22, 23], vivo.Avisados.OrderBy(i => i));
        Assert.Contains((7, "ACTIVA", 2), vivo.Actualizadas);   // comunidadActualizada de la que cambió de creador
        Assert.Contains((99, "ACTIVA", 2), vivo.Actualizadas);  // cupo de una comunidad ajena de la que se lo quitó
    }
}
