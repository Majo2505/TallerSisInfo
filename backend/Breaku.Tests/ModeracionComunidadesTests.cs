using Breaku.Application.Admin;
using Breaku.Application.Horarios;
using Breaku.Application.TiempoReal;
using Breaku.Domain;

namespace Breaku.Tests;

/// <summary>HU35: el admin quita y restaura comunidades. Quitar no sanciona a quien la creó.</summary>
public sealed class ModeracionComunidadesTests
{
    private static readonly DateTime Ahora = new(2026, 11, 24, 15, 0, 0, DateTimeKind.Utc);

    private sealed class RelojFijo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Ahora, TimeSpan.Zero);
    }

    /// <summary>Una comunidad en memoria con las reglas de estado del repositorio real.</summary>
    private sealed class RepoFalso : IModeracionComunidadRepository
    {
        public string Estado { get; set; } = ComunidadEfimera.EstadoActiva;
        public string? Motivo { get; private set; }
        public string? AvisoEnviado { get; private set; }
        public string? RespuestaReporte { get; private set; }
        public int? ReporteResuelto { get; private set; }
        public (int Id, int ComunidadId, string Estado)? Reporte { get; init; }
        public int Sanciones { get; } = 0; // la moderación nunca crea sanciones
        public string? EstadoPedido { get; private set; }

        public Task<List<FilaComunidadAdmin>> ListarAsync(string estado, DateTime ahoraUtc, int maximo, CancellationToken ct = default)
        {
            EstadoPedido = estado;
            return Task.FromResult(new List<FilaComunidadAdmin>
            {
                new(1, "jugar cartas", 6, "Mateo Vargas", Ahora.AddHours(2), Ahora.AddHours(3), 2, true, 4, false, estado, 3,
                    estado == ComunidadEfimera.EstadoBajada ? "Spam" : null, estado == ComunidadEfimera.EstadoBajada ? Ahora : null)
            });
        }

        public Task<CambioComunidad> QuitarAsync(int comunidadId, int adminId, string motivo, Func<string, string> aviso, int? reporteId,
            string respuestaReporte, DateTime ahoraUtc, CancellationToken ct = default)
        {
            if (comunidadId != 1) return Task.FromResult(new CambioComunidad(ResultadoModeracionComunidad.NoEncontrada, [], 0, null));
            if (Estado != ComunidadEfimera.EstadoActiva)
                return Task.FromResult(new CambioComunidad(ResultadoModeracionComunidad.EstadoInvalido, [], 0, null));
            if (reporteId is int r)
            {
                if (Reporte is null || Reporte.Value.Id != r || Reporte.Value.ComunidadId != comunidadId)
                    return Task.FromResult(new CambioComunidad(ResultadoModeracionComunidad.ReporteNoCorresponde, [], 0, null));
                if (Reporte.Value.Estado != "PENDIENTE")
                    return Task.FromResult(new CambioComunidad(ResultadoModeracionComunidad.ReporteYaRevisado, [], 0, null));
                ReporteResuelto = r;
                RespuestaReporte = respuestaReporte;
            }
            Estado = ComunidadEfimera.EstadoBajada;
            Motivo = motivo;
            AvisoEnviado = aviso("jugar cartas");
            return Task.FromResult(new CambioComunidad(ResultadoModeracionComunidad.Aplicado, reporteId is null ? [6, 8] : [6, 8, 3], 0, reporteId));
        }

        public Task<CambioComunidad> RestaurarAsync(int comunidadId, Func<string, string> aviso, DateTime ahoraUtc, CancellationToken ct = default)
        {
            if (Estado != ComunidadEfimera.EstadoBajada)
                return Task.FromResult(new CambioComunidad(ResultadoModeracionComunidad.EstadoInvalido, [], 0, null));
            Estado = ComunidadEfimera.EstadoActiva;
            Motivo = null;
            AvisoEnviado = aviso("jugar cartas");
            return Task.FromResult(new CambioComunidad(ResultadoModeracionComunidad.Aplicado, [6, 8], 2, null));
        }
    }

    private sealed class EnVivoFalso : ITiempoReal
    {
        public List<string> Hechos { get; } = [];
        public Task AvisosCambiaronAsync(IReadOnlyCollection<int> usuarioIds, CancellationToken ct = default)
        { Hechos.Add($"avisos:{string.Join(",", usuarioIds)}"); return Task.CompletedTask; }
        public Task ComunidadCreadaAsync(int comunidadId, CancellationToken ct = default)
        { Hechos.Add($"descubrir:{comunidadId}"); return Task.CompletedTask; }
        public Task AvisosDeComunidadAsync(int comunidadId, CancellationToken ct = default) => Task.CompletedTask;
        public Task ComunidadActualizadaAsync(int comunidadId, string estado, int confirmados, CancellationToken ct = default)
        { Hechos.Add($"comunidad:{comunidadId}:{estado}:{confirmados}"); return Task.CompletedTask; }
    }

    private static (ModeracionComunidadesService Servicio, RepoFalso Repo, EnVivoFalso EnVivo, AvisosAdminTests.AvisosFalsos Avisos) Crear(
        RepoFalso? repo = null)
    {
        repo ??= new RepoFalso();
        var enVivo = new EnVivoFalso();
        var avisos = new AvisosAdminTests.AvisosFalsos();
        return (new ModeracionComunidadesService(repo, new RelojFijo(), enVivo, avisos), repo, enVivo, avisos);
    }

    // ── Reglas ──

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void El_motivo_es_obligatorio(string? motivo)
        => Assert.Equal(ReglasModeracionComunidad.MensajeFaltaMotivo, ReglasModeracionComunidad.ValidarMotivo(motivo).Error);

    [Fact]
    public void El_motivo_llega_a_500_caracteres()
    {
        Assert.Null(ReglasModeracionComunidad.ValidarMotivo(new string('m', 500)).Error);
        Assert.Equal(ReglasModeracionComunidad.MensajeMotivoLargo, ReglasModeracionComunidad.ValidarMotivo(new string('m', 501)).Error);
    }

    [Fact]
    public void Los_avisos_llevan_el_motivo_y_caben_en_200_caracteres()
    {
        Assert.Equal("«jugar cartas» fue retirada por moderación. Motivo: Contenido ofensivo",
            ReglasModeracionComunidad.AvisoBajada(" jugar cartas ", "Contenido ofensivo"));
        Assert.True(ReglasModeracionComunidad.AvisoBajada(new string('a', 100), new string('m', 500)).Length <= 200);
        Assert.Equal("«jugar cartas» volvió a estar disponible.", ReglasModeracionComunidad.AvisoRestaurada("jugar cartas"));
        Assert.True(ReglasModeracionComunidad.RespuestaReporte(new string('m', 500)).Length <= 500);
    }

    // ── Quitar ──

    [Fact]
    public async Task Quitar_la_deja_bajada_con_motivo_avisa_y_no_sanciona()
    {
        var (servicio, repo, enVivo, _) = Crear();

        await servicio.QuitarAsync(1, 2, new QuitarComunidadRequest("  Contenido ofensivo  "));

        Assert.Equal(ComunidadEfimera.EstadoBajada, repo.Estado);
        Assert.Equal("Contenido ofensivo", repo.Motivo);
        Assert.Equal("«jugar cartas» fue retirada por moderación. Motivo: Contenido ofensivo", repo.AvisoEnviado);
        Assert.Equal(0, repo.Sanciones);
        Assert.Equal(["avisos:6,8", "comunidad:1:BAJADA:0", "descubrir:1"], enVivo.Hechos);
    }

    [Fact]
    public async Task Quitar_sin_motivo_es_un_error_de_validacion_y_no_cambia_nada()
    {
        var (servicio, repo, _, _) = Crear();

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.QuitarAsync(1, 2, new QuitarComunidadRequest(" ")));

        Assert.Equal(ReglasModeracionComunidad.ErrorValidacion, error.Codigo);
        Assert.Equal(ComunidadEfimera.EstadoActiva, repo.Estado);
    }

    [Fact]
    public async Task Quitar_una_que_no_esta_activa_es_un_conflicto_y_si_no_existe_404()
    {
        var (servicio, _, _, _) = Crear(new RepoFalso { Estado = ComunidadEfimera.EstadoArchivada });

        var conflicto = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.QuitarAsync(1, 2, new QuitarComunidadRequest("Spam")));
        var noExiste = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.QuitarAsync(99, 2, new QuitarComunidadRequest("Spam")));

        Assert.Equal((ReglasModeracionComunidad.ErrorEstado, ReglasModeracionComunidad.MensajeNoActiva), (conflicto.Codigo, conflicto.Message));
        Assert.Equal(ReglasModeracionComunidad.ErrorNoEncontrada, noExiste.Codigo);
    }

    [Fact]
    public async Task Desde_un_reporte_lo_deja_resuelto_avisa_a_quien_reporto_y_cierra_el_aviso_de_los_admins()
    {
        var (servicio, repo, enVivo, avisos) = Crear(new RepoFalso { Reporte = (31, 1, "PENDIENTE") });

        await servicio.QuitarAsync(1, 2, new QuitarComunidadRequest("Spam", 31));

        Assert.Equal(31, repo.ReporteResuelto);
        Assert.Equal("Quitamos la comunidad. Motivo: Spam", repo.RespuestaReporte);
        Assert.Contains("avisos:6,8,3", enVivo.Hechos);
        Assert.Equal(["reporte-revisado:31"], avisos.Llamadas);
    }

    [Fact]
    public async Task Un_reporte_de_otra_comunidad_o_ya_revisado_no_deja_quitar()
    {
        var (otro, repoOtro, _, _) = Crear(new RepoFalso { Reporte = (31, 7, "PENDIENTE") });
        var e1 = await Assert.ThrowsAsync<ReglaNegocioException>(() => otro.QuitarAsync(1, 2, new QuitarComunidadRequest("Spam", 31)));
        Assert.Equal(ReglasModeracionComunidad.ErrorValidacion, e1.Codigo);
        Assert.Equal(ComunidadEfimera.EstadoActiva, repoOtro.Estado);

        var (revisado, _, _, _) = Crear(new RepoFalso { Reporte = (31, 1, "RESUELTO") });
        var e2 = await Assert.ThrowsAsync<ReglaNegocioException>(() => revisado.QuitarAsync(1, 2, new QuitarComunidadRequest("Spam", 31)));
        Assert.Equal(ReglasModeracionComunidad.ErrorReporteYaRevisado, e2.Codigo);
    }

    // ── Restaurar ──

    [Fact]
    public async Task Restaurar_la_vuelve_activa_y_avisa()
    {
        var (servicio, repo, enVivo, _) = Crear(new RepoFalso { Estado = ComunidadEfimera.EstadoBajada });

        await servicio.RestaurarAsync(1);

        Assert.Equal(ComunidadEfimera.EstadoActiva, repo.Estado);
        Assert.Equal("«jugar cartas» volvió a estar disponible.", repo.AvisoEnviado);
        Assert.Equal(["avisos:6,8", "comunidad:1:ACTIVA:2", "descubrir:1"], enVivo.Hechos);
    }

    [Fact]
    public async Task Solo_se_restaura_una_bajada()
    {
        var (servicio, _, _, _) = Crear();
        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.RestaurarAsync(1));
        Assert.Equal((ReglasModeracionComunidad.ErrorEstado, ReglasModeracionComunidad.MensajeNoBajada), (error.Codigo, error.Message));
    }

    // ── Listado ──

    [Fact]
    public async Task El_listado_pide_activas_por_defecto_y_rechaza_otro_estado()
    {
        var (servicio, repo, _, _) = Crear();

        var activas = await servicio.ListarAsync(null);
        Assert.Equal(ComunidadEfimera.EstadoActiva, repo.EstadoPedido);
        var c = Assert.Single(activas);
        Assert.Equal((2, 3, "Mateo Vargas"), (c.Confirmados, c.ReportesPendientes, c.Creador));
        Assert.Equal(TimeSpan.FromHours(-4), c.Inicio.Offset);

        var bajadas = await servicio.ListarAsync("bajada");
        Assert.Equal("Spam", Assert.Single(bajadas).MotivoBaja);

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.ListarAsync("ARCHIVADA"));
        Assert.Equal(ReglasModeracionComunidad.ErrorValidacion, error.Codigo);
    }
}
