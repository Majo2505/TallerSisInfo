using System.Diagnostics;
using Breaku.Application.Admin;
using Breaku.Application.Horarios;
using Breaku.Application.Puentes;
using Breaku.Application.Recomendaciones;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Tests;

/// <summary>HU19: la recomendación responde en menos de 3,5 s aunque la IA tarde o se caiga.</summary>
public class RendimientoTests
{
    // ---------- plan B: los más cercanos de lo ya recuperado

    private static ReglasRendimiento.Candidato C(int id, int traslado, int metros, int duracion) => new(id, traslado, metros, duracion);

    [Fact]
    public void El_plan_b_prefiere_lo_que_entra_en_el_puente_y_despues_lo_mas_cercano()
    {
        var elegidos = ReglasRendimiento.Respaldo(
        [
            C(1, traslado: 2, metros: 150, duracion: 90),   // muy cerca, pero no entra en 60 min
            C(2, traslado: 8, metros: 600, duracion: 30),
            C(3, traslado: 4, metros: 300, duracion: 30),
            C(4, traslado: 4, metros: 250, duracion: 20),
            C(5, traslado: 10, metros: 800, duracion: 20)
        ], minutosDisponibles: 60);

        Assert.Equal([4, 3, 2], elegidos.Select(e => e.LugarId));
    }

    [Fact]
    public void El_plan_b_no_repite_lugares_ni_pasa_del_maximo()
    {
        var elegidos = ReglasRendimiento.Respaldo([C(1, 2, 100, 10), C(1, 2, 100, 10), C(2, 3, 200, 10)], 60);

        Assert.Equal([1, 2], elegidos.Select(e => e.LugarId));
        Assert.True(ReglasRendimiento.Respaldo(Enumerable.Range(1, 9).Select(i => C(i, i, i * 10, 10)), 120).Count
                    <= ReglasRecomendacion.MaximoRecomendaciones);
    }

    [Fact]
    public void Las_explicaciones_del_plan_b_pasan_las_mismas_reglas_que_las_de_la_ia()
    {
        foreach (var (_, explicacion) in ReglasRendimiento.Respaldo([C(1, 2, 100, 10), C(2, 3, 200, 10), C(3, 4, 300, 10)], 60))
        {
            Assert.True(ReglasRecomendacion.ExplicacionEsSegura(explicacion, "Cafetería Central"));
            Assert.DoesNotMatch(@"\d", explicacion); // regla 3 del prompt: sin cifras, la app ya las muestra
        }
    }

    // ---------- el servicio no espera de más

    [Fact]
    public async Task Si_la_ia_tarda_responde_el_plan_b_dentro_del_presupuesto()
    {
        var (servicio, repo) = Armar(new AsistenteLento(TimeSpan.FromSeconds(10)), presupuesto: TimeSpan.FromMilliseconds(150));

        var reloj = Stopwatch.StartNew();
        var r = await servicio.RecomendarAsync(7, new SolicitudRecomendacionDto(5, "algo tranquilo"));
        reloj.Stop();

        Assert.True(reloj.ElapsedMilliseconds < 2000, $"tardó {reloj.ElapsedMilliseconds} ms");
        Assert.Equal(RecomendacionService.EstadoRespuestaOk, r.Estado);
        Assert.Equal(ReglasRendimiento.MensajeRespaldo, r.Mensaje);
        Assert.Equal([2, 1], r.Recomendaciones.Select(x => x.LugarId));
        var log = Assert.Single(repo.Logs);
        Assert.Equal(LogAuditoriaRag.EstadoOk, log.Estado);
        Assert.Equal(ReglasRendimiento.ModeloRespaldo, log.Modelo);
        Assert.Equal(2, repo.Recomendaciones.Count);
    }

    [Fact]
    public async Task Si_la_ia_se_cae_tambien_hay_respuesta()
    {
        var (servicio, repo) = Armar(new AsistenteCaido());

        var r = await servicio.RecomendarAsync(7, new SolicitudRecomendacionDto(5, null));

        Assert.Equal(RecomendacionService.EstadoRespuestaOk, r.Estado);
        Assert.NotEmpty(r.Recomendaciones);
        Assert.Equal(ReglasRendimiento.ModeloRespaldo, Assert.Single(repo.Logs).Modelo);
    }

    [Fact]
    public async Task Si_la_ia_contesta_a_tiempo_se_usa_su_respuesta()
    {
        var (servicio, repo) = Armar(new AsistenteLento(TimeSpan.Zero));

        var r = await servicio.RecomendarAsync(7, new SolicitudRecomendacionDto(5, null));

        Assert.Null(r.Mensaje);
        Assert.Equal("Buen lugar para un café", Assert.Single(r.Recomendaciones).Explicacion);
        Assert.Equal("falso", Assert.Single(repo.Logs).Modelo);
    }

    [Fact]
    public async Task Si_el_estudiante_cancela_no_se_inventa_una_respuesta()
    {
        var (servicio, repo) = Armar(new AsistenteLento(TimeSpan.FromSeconds(10)));
        using var cancelar = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => servicio.RecomendarAsync(7, new SolicitudRecomendacionDto(5, null), cancelar.Token));
        Assert.Empty(repo.Logs);
    }

    // ---------- la tarjeta de Métricas

    [Fact]
    public void Metricas_p50_p95_porcentaje_a_tiempo_y_respaldos()
    {
        var r = RendimientoRecomendacion.Calcular(
            Enumerable.Range(1, 18).Select(i => (i * 100, false)).Append((3600, true)).Append((4000, false)).ToList());

        Assert.Equal(20, r.Consultas);
        Assert.Equal(3500, r.ObjetivoMs);
        Assert.Equal(1000, r.P50Ms);
        Assert.Equal(3600, r.P95Ms);
        Assert.Equal(90, r.DentroDelObjetivoPct);
        Assert.Equal(1, r.Respaldos);
    }

    [Fact]
    public void Metricas_sin_recomendaciones()
    {
        var r = RendimientoRecomendacion.Calcular([]);

        Assert.Equal(0, r.Consultas);
        Assert.Null(r.P95Ms);
        Assert.Null(r.DentroDelObjetivoPct);
    }

    [Theory]
    [InlineData(3499, true)]
    [InlineData(3500, false)]
    public void El_objetivo_es_menos_de_3_5_segundos(int ms, bool aTiempo)
        => Assert.Equal(aTiempo, ReglasRendimiento.DentroDelObjetivo(ms));

    // ---------- falsos

    private static (RecomendacionService, Repositorio) Armar(IAsistenteIa asistente, TimeSpan? presupuesto = null)
    {
        var repo = new Repositorio();
        var servicio = presupuesto is null
            ? new RecomendacionService(new Puentes(), new Recuperador(), asistente, repo)
            : new RecomendacionService(new Puentes(), new Recuperador(), asistente, repo, presupuesto.Value);
        return (servicio, repo);
    }

    private sealed class Puentes : IPuenteRepository
    {
        public Task<List<Puente>> ListarPorUsuarioAsync(int usuarioId, CancellationToken ct = default)
            => Task.FromResult(new List<Puente>
            {
                new() { Id = 5, UsuarioId = usuarioId, DiaSemana = 1, HoraInicio = new TimeOnly(9, 30), HoraFin = new TimeOnly(10, 30) }
            });

        public Task ReemplazarAsync(int usuarioId, IReadOnlyList<Puente> nuevos, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class Recuperador : IRecuperadorLugares
    {
        public Task<ContextoRecuperado> RecuperarAsync(int usuarioId, Puente puente, CancellationToken ct = default)
            => Task.FromResult(new ContextoRecuperado(1, 800,
            [
                new LugarCandidato(1, "Cafetería Central", "Comida", "Café y masitas.", 300, 5, 20),
                new LugarCandidato(2, "Biblioteca", "Estudio", "Silencio.", 120, 2, 30)
            ]));
    }

    private sealed class AsistenteLento(TimeSpan demora) : IAsistenteIa
    {
        public async Task<RespuestaAsistente> GenerarAsync(PromptRag prompt, CancellationToken ct = default)
        {
            await Task.Delay(demora, ct);
            return new RespuestaAsistente("falso", "{}", [new SugerenciaAsistente(1, "Buen lugar para un café")]);
        }
    }

    private sealed class AsistenteCaido : IAsistenteIa
    {
        public Task<RespuestaAsistente> GenerarAsync(PromptRag prompt, CancellationToken ct = default)
            => throw new HttpRequestException("El LLM respondió 500.");
    }

    private sealed class Repositorio : IRecomendacionRepository
    {
        public List<LogAuditoriaRag> Logs { get; } = [];
        public List<Recomendacion> Recomendaciones { get; } = [];

        public Task GuardarAsync(LogAuditoriaRag log, IReadOnlyList<Recomendacion> recomendaciones, CancellationToken ct = default)
        {
            Logs.Add(log);
            Recomendaciones.AddRange(recomendaciones);
            return Task.CompletedTask;
        }

        public Task<Recomendacion?> ObtenerDeUsuarioAsync(long recomendacionId, int usuarioId, CancellationToken ct = default)
            => Task.FromResult<Recomendacion?>(null);

        public Task GuardarFeedbackAsync(Recomendacion recomendacion, string feedback, CancellationToken ct = default) => Task.CompletedTask;
    }
}
