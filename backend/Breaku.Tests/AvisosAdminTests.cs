using Breaku.Application.Notificaciones;
using Breaku.Application.Reportes;
using Breaku.Application.TiempoReal;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Tests;

/// <summary>Campanita del admin: reportes nuevos y eventos por revisar (migración 030).</summary>
public class AvisosAdminTests
{
    /// <summary>Lo que se pidió avisar o cerrar, en orden.</summary>
    internal sealed class AvisosFalsos : IAvisosAdmin
    {
        public List<string> Llamadas { get; } = [];
        public Task ReporteNuevoAsync(int reporteId, string motivo, string? descripcion, CancellationToken ct = default)
        { Llamadas.Add($"reporte-nuevo:{reporteId}:{motivo}"); return Task.CompletedTask; }
        public Task ReporteRevisadoAsync(int reporteId, CancellationToken ct = default)
        { Llamadas.Add($"reporte-revisado:{reporteId}"); return Task.CompletedTask; }
        public Task EventoPorRevisarAsync(int eventoId, string titulo, string? motivo, CancellationToken ct = default)
        { Llamadas.Add($"evento-por-revisar:{eventoId}"); return Task.CompletedTask; }
        public Task EventoRevisadoAsync(int eventoId, CancellationToken ct = default)
        { Llamadas.Add($"evento-revisado:{eventoId}"); return Task.CompletedTask; }
        public Task IntentoBloqueadoAsync(int usuarioId, CancellationToken ct = default)
        { Llamadas.Add($"intento-bloqueado:{usuarioId}"); return Task.CompletedTask; }
        public Task ReportesAcumuladosAsync(int reporteId, CancellationToken ct = default)
        { Llamadas.Add($"reportes-acumulados:{reporteId}"); return Task.CompletedTask; }
        public Task AlertasResueltasAsync(int usuarioId, CancellationToken ct = default)
        { Llamadas.Add($"alertas-resueltas:{usuarioId}"); return Task.CompletedTask; }
    }

    private sealed class RepoAvisos : IAvisosAdminRepository
    {
        public bool Falla { get; init; }
        public List<(string Tipo, ObjetoAvisoAdmin Objeto, int Id, string? Mensaje)> Hechos { get; } = [];
        public Task<IReadOnlyList<int>> AvisarAsync(string tipo, ObjetoAvisoAdmin objeto, int objetoId, string mensaje, CancellationToken ct = default)
        {
            if (Falla) throw new InvalidOperationException("Check constraint 'ck_notif_tipo' is violated.");
            Hechos.Add((tipo, objeto, objetoId, mensaje));
            return Task.FromResult<IReadOnlyList<int>>([2, 9]);
        }
        public Task<IReadOnlyList<int>> CerrarAsync(string tipo, ObjetoAvisoAdmin objeto, int objetoId, CancellationToken ct = default)
        {
            Hechos.Add((tipo, objeto, objetoId, null));
            return Task.FromResult<IReadOnlyList<int>>([]);
        }

        // HU32
        public CandidatoAlerta? Candidato { get; init; }
        public DateTime? DesdePedido { get; private set; }
        public List<(string Tipo, int Objetivo, string Mensaje)> Alertas { get; } = [];
        public Task<CandidatoAlerta?> IntentosBloqueadosAsync(int usuarioId, DateTime desdeUtc, CancellationToken ct = default)
        {
            if (Falla) throw new InvalidOperationException("Unknown column 'usuario_objetivo_id'");
            DesdePedido = desdeUtc;
            return Task.FromResult(Candidato);
        }
        public Task<CandidatoAlerta?> CreadorReportadoAsync(int reporteId, CancellationToken ct = default)
            => Task.FromResult(Candidato);
        public Task<IReadOnlyList<int>> AlertarAsync(string tipo, int usuarioObjetivoId, string mensaje, CancellationToken ct = default)
        {
            Alertas.Add((tipo, usuarioObjetivoId, mensaje));
            return Task.FromResult<IReadOnlyList<int>>([2]);
        }
        public List<FilaAlerta> Activas { get; init; } = [];
        public List<int> Resueltas { get; } = [];
        public Task<List<FilaAlerta>> ListarAlertasActivasAsync(CancellationToken ct = default) => Task.FromResult(Activas);
        public Task<IReadOnlyList<int>> ResolverAlertasAsync(int usuarioObjetivoId, CancellationToken ct = default)
        {
            Resueltas.Add(usuarioObjetivoId);
            return Task.FromResult<IReadOnlyList<int>>([2, 9]);
        }
    }

    private sealed class EnVivoFalso : ITiempoReal
    {
        public List<int> Avisados { get; } = [];
        public Task AvisosCambiaronAsync(IReadOnlyCollection<int> usuarioIds, CancellationToken ct = default)
        { Avisados.AddRange(usuarioIds); return Task.CompletedTask; }
        public Task ComunidadCreadaAsync(int comunidadId, CancellationToken ct = default) => Task.CompletedTask;
        public Task AvisosDeComunidadAsync(int comunidadId, CancellationToken ct = default) => Task.CompletedTask;
        public Task ComunidadActualizadaAsync(int comunidadId, string estado, int confirmados, CancellationToken ct = default) => Task.CompletedTask;
    }

    // ── Textos ──

    [Fact]
    public void El_aviso_de_reporte_dice_el_motivo_y_cita_la_descripcion()
    {
        var m = ReglasAvisosAdmin.MensajeReporteNuevo(ReglasReporte.MotivoLugarIncorrecto, "  Dice que abre a las 7\ny abre a las 9 ");
        Assert.Equal("Nuevo reporte «Información de un lugar». «Dice que abre a las 7 y abre a las 9»", m);
    }

    [Fact]
    public void Un_aviso_largo_se_recorta_a_200_caracteres()
    {
        var m = ReglasAvisosAdmin.MensajeEventoPorRevisar(new string('T', 150), new string('x', 500));
        Assert.True(m.Length <= ReglasAvisosAdmin.LargoMaximo);
        Assert.Contains("…", m);
        Assert.Contains("espera tu revisión", m);
    }

    [Fact]
    public void Sin_motivo_el_aviso_de_evento_no_lleva_cita()
        => Assert.Equal("«Feria de ciencias» espera tu revisión.", ReglasAvisosAdmin.MensajeEventoPorRevisar(" Feria de ciencias ", null));

    // ── Servicio ──

    [Fact]
    public async Task Un_reporte_nuevo_avisa_a_los_admins_en_vivo()
    {
        var repo = new RepoAvisos();
        var enVivo = new EnVivoFalso();
        await new AvisosAdminService(repo, enVivo).ReporteNuevoAsync(12, ReglasReporte.MotivoErrorApp, "No carga el horario");

        var hecho = Assert.Single(repo.Hechos);
        Assert.Equal((Notificacion.TipoReporteNuevo, ObjetoAvisoAdmin.Reporte, 12), (hecho.Tipo, hecho.Objeto, hecho.Id));
        Assert.Equal([2, 9], enVivo.Avisados);
    }

    [Fact]
    public async Task Cerrar_un_evento_revisado_apunta_al_evento()
    {
        var repo = new RepoAvisos();
        await new AvisosAdminService(repo, new EnVivoFalso()).EventoRevisadoAsync(5);

        var hecho = Assert.Single(repo.Hechos);
        Assert.Equal((Notificacion.TipoEventoPorRevisar, ObjetoAvisoAdmin.Evento, 5), (hecho.Tipo, hecho.Objeto, hecho.Id));
    }

    [Fact]
    public async Task Si_la_base_no_tiene_la_030_el_aviso_falla_en_silencio()
    {
        var enVivo = new EnVivoFalso();
        await new AvisosAdminService(new RepoAvisos { Falla = true }, enVivo).EventoPorRevisarAsync(5, "Feria", null);
        Assert.Empty(enVivo.Avisados);
    }

    // ── Reportes ──

    private sealed class RepoReportes : IReporteRepository
    {
        public Task<List<LugarReportableDto>> ListarLugaresAsync(CancellationToken ct = default) => Task.FromResult(new List<LugarReportableDto>());
        public Task<bool> ObjetoExisteAsync(string objeto, int id, CancellationToken ct = default) => Task.FromResult(true);
        public Task<int> ContarPendientesAsync(int usuarioId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<bool> TienePendienteSobreAsync(int usuarioId, string objeto, int id, CancellationToken ct = default) => Task.FromResult(false);
        public Task<int> CrearAsync(int usuarioId, string motivo, int? lugarId, int? comunidadId, int? eventoId, string descripcion,
            CancellationToken ct = default) => Task.FromResult(31);
        public Task<List<MiReporteDto>> ListarMiosAsync(int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    [Fact]
    public async Task Enviar_un_reporte_avisa_a_los_admins()
    {
        var avisos = new AvisosFalsos();
        await new ReporteService(new RepoReportes(), avisos).CrearAsync(1, new NuevoReporteDto("ERROR_APP", null, "El botón de guardar no responde"));
        Assert.Equal(["reporte-nuevo:31:ERROR_APP", "reportes-acumulados:31"], avisos.Llamadas);
    }

    [Fact]
    public async Task Un_reporte_invalido_no_avisa()
    {
        var avisos = new AvisosFalsos();
        await Assert.ThrowsAsync<Breaku.Application.Horarios.ReglaNegocioException>(
            () => new ReporteService(new RepoReportes(), avisos).CrearAsync(1, new NuevoReporteDto("ERROR_APP", null, "corto")));
        Assert.Empty(avisos.Llamadas);
    }

    // ── HU32: alertas ──

    private sealed class RelojFijo(DateTime ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(ahora, TimeSpan.Zero);
    }

    private static readonly DateTime Ahora = new(2026, 11, 23, 15, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(2, false, false)]
    [InlineData(3, false, true)]
    [InlineData(7, false, true)]
    [InlineData(5, true, false)]
    public void La_alerta_por_intentos_salta_desde_el_tercero_y_nunca_sobre_un_admin(int intentos, bool esAdmin, bool alerta)
        => Assert.Equal(alerta, ReglasAlertasAdmin.AlertarPorIntentos(intentos, esAdmin));

    [Theory]
    [InlineData(2, false, false)]
    [InlineData(3, false, true)]
    [InlineData(4, true, false)]
    public void La_alerta_por_reportes_pide_tres_personas_distintas_y_nunca_sobre_un_admin(int personas, bool esAdmin, bool alerta)
        => Assert.Equal(alerta, ReglasAlertasAdmin.AlertarPorReportes(personas, esAdmin));

    [Fact]
    public void La_ventana_es_de_24_horas()
        => Assert.Equal(Ahora.AddHours(-24), ReglasAlertasAdmin.InicioVentana(Ahora));

    [Theory]
    [InlineData(null, true)]
    [InlineData("manual", true)]
    [InlineData("gpt-5-mini", false)]
    public void Solo_cuenta_lo_que_escribio_el_estudiante(string? modelo, bool cuenta)
        => Assert.Equal(cuenta, ReglasAlertasAdmin.CuentaComoIntento(modelo, "manual"));

    [Fact]
    public void Los_mensajes_de_alerta_caben_y_recortan_un_nombre_largo()
    {
        Assert.Equal("Ana Pérez tuvo 3 intentos bloqueados por seguridad en las últimas 24 horas.",
            ReglasAlertasAdmin.MensajeSospechoso(" Ana Pérez ", 3));
        Assert.Equal("Ana Pérez tuvo 7 intentos bloqueados por seguridad en las últimas 24 horas; 2 desde su última advertencia.",
            ReglasAlertasAdmin.MensajeSospechoso("Ana Pérez", 7, 2));
        Assert.EndsWith("; 1 desde su última advertencia.", ReglasAlertasAdmin.MensajeSospechoso("Ana", 4, 1));
        Assert.True(ReglasAlertasAdmin.MensajeSospechoso(new string('N', 150), 999, 999).Length <= ReglasAvisosAdmin.LargoMaximo);
        var largo = ReglasAlertasAdmin.MensajeReportes(new string('N', 150), 12);
        Assert.True(largo.Length <= ReglasAvisosAdmin.LargoMaximo);
        Assert.Contains("…", largo);
    }

    [Fact]
    public async Task Tres_intentos_en_24_horas_alertan_a_los_admins_con_el_usuario_objetivo()
    {
        var repo = new RepoAvisos { Candidato = new CandidatoAlerta(7, "Ana Pérez", false, 3, 2) };
        var enVivo = new EnVivoFalso();
        await new AvisosAdminService(repo, enVivo, new RelojFijo(Ahora)).IntentoBloqueadoAsync(7);

        Assert.Equal(Ahora.AddHours(-24), repo.DesdePedido);
        var alerta = Assert.Single(repo.Alertas);
        Assert.Equal((Notificacion.TipoAlertaUsuarioSospechoso, 7), (alerta.Tipo, alerta.Objetivo));
        Assert.EndsWith("; 2 desde su última advertencia.", alerta.Mensaje);
        Assert.Equal([2], enVivo.Avisados);
    }

    [Fact]
    public async Task Con_dos_intentos_o_si_es_admin_no_alerta()
    {
        foreach (var c in new[] { new CandidatoAlerta(7, "Ana", false, 2), new CandidatoAlerta(2, "Admin", true, 9) })
        {
            var repo = new RepoAvisos { Candidato = c };
            await new AvisosAdminService(repo, new EnVivoFalso(), new RelojFijo(Ahora)).IntentoBloqueadoAsync(c.UsuarioId);
            Assert.Empty(repo.Alertas);
        }
    }

    [Fact]
    public async Task Reportes_de_tres_personas_alertan_sobre_quien_creo_lo_reportado()
    {
        var repo = new RepoAvisos { Candidato = new CandidatoAlerta(9, "Diego Mamani", false, 3) };
        await new AvisosAdminService(repo, new EnVivoFalso(), new RelojFijo(Ahora)).ReportesAcumuladosAsync(31);

        var alerta = Assert.Single(repo.Alertas);
        Assert.Equal((Notificacion.TipoAlertaReportesAcumulados, 9), (alerta.Tipo, alerta.Objetivo));
        Assert.Contains("3 personas distintas", alerta.Mensaje);
    }

    [Fact]
    public async Task Si_falla_la_alerta_no_rompe_lo_que_hacia_el_estudiante()
    {
        var enVivo = new EnVivoFalso();
        await new AvisosAdminService(new RepoAvisos { Falla = true }, enVivo, new RelojFijo(Ahora)).IntentoBloqueadoAsync(7);
        Assert.Empty(enVivo.Avisados);
    }

    [Fact]
    public async Task La_pestana_alertas_agrupa_por_estudiante_del_mas_reciente_al_mas_antiguo()
    {
        var repo = new RepoAvisos
        {
            Activas =
            [
                new FilaAlerta(7, "Ana", "ana@ucb.edu.bo", true, Notificacion.TipoAlertaUsuarioSospechoso, "3 intentos", Ahora.AddHours(-5)),
                new FilaAlerta(9, "Diego", "diego@ucb.edu.bo", true, Notificacion.TipoAlertaReportesAcumulados, "reportes", Ahora.AddHours(-1)),
                new FilaAlerta(7, "Ana", "ana@ucb.edu.bo", true, Notificacion.TipoAlertaReportesAcumulados, "reportes", Ahora.AddHours(-3))
            ]
        };

        var lista = await new AlertasAdminService(repo, new EnVivoFalso()).ListarAsync();

        Assert.Equal([9, 7], lista.Select(a => a.UsuarioId));
        var ana = lista[1];
        Assert.Equal([Notificacion.TipoAlertaReportesAcumulados, Notificacion.TipoAlertaUsuarioSospechoso], ana.Alertas.Select(a => a.Tipo));
        Assert.Equal(Ahora.AddHours(-3), ana.Ultima);
    }

    [Fact]
    public async Task Descartar_resuelve_las_alertas_y_avisa_a_las_campanitas()
    {
        var repo = new RepoAvisos();
        var enVivo = new EnVivoFalso();

        await new AlertasAdminService(repo, enVivo).DescartarAsync(7);

        Assert.Equal([7], repo.Resueltas);
        Assert.Equal([2, 9], enVivo.Avisados);
    }

    [Fact]
    public async Task Sancionar_tambien_resuelve_las_alertas()
    {
        var repo = new RepoAvisos();
        await new AvisosAdminService(repo, new EnVivoFalso(), new RelojFijo(Ahora)).AlertasResueltasAsync(7);
        Assert.Equal([7], repo.Resueltas);
    }
}

