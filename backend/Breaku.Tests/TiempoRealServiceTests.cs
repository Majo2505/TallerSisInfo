using Breaku.Application.TiempoReal;
using Breaku.Domain;
using Microsoft.Extensions.Logging.Abstractions;

namespace Breaku.Tests;

/// <summary>HU18/HU20: Application decide a quién avisar; el canal (SignalR) solo envía.</summary>
public class TiempoRealServiceTests
{
    private static (TiempoRealService, Canal, ComunidadServiceTests.RepositorioFalso) Armar()
    {
        var canal = new Canal();
        var repo = new ComunidadServiceTests.RepositorioFalso
        {
            Comunidad = new ComunidadEfimera { Id = 12, CreadorId = ComunidadServiceTests.Yo, Actividad = "jugar cartas" }
        };
        repo.YaDentro.AddRange([ComunidadServiceTests.Yo, 30, 31, 30]);
        return (new TiempoRealService(canal, repo, NullLogger<TiempoRealService>.Instance), canal, repo);
    }

    [Fact]
    public async Task Una_comunidad_nueva_se_avisa_en_la_sede_de_quien_la_creo_y_a_quienes_estan_en_ella()
    {
        var (servicio, canal, _) = Armar();

        await servicio.ComunidadCreadaAsync(12);

        Assert.Equal([3], canal.Sedes);   // la sede del creador (zona 3 en el repositorio falso)
        Assert.Equal([ComunidadServiceTests.Yo, 30, 31], Assert.Single(canal.Avisos));
    }

    [Fact]
    public async Task Una_comunidad_que_no_existe_no_avisa_a_ninguna_sede()
    {
        var (servicio, canal, _) = Armar();

        await servicio.ComunidadCreadaAsync(99);

        Assert.Empty(canal.Sedes);
    }

    [Fact]
    public async Task Sin_destinatarios_no_se_manda_nada()
    {
        var (servicio, canal, repo) = Armar();
        repo.YaDentro.Clear();

        await servicio.AvisosDeComunidadAsync(12);
        await servicio.AvisosCambiaronAsync([]);

        Assert.Empty(canal.Avisos);
    }

    [Fact]
    public async Task Si_el_canal_falla_la_operacion_sigue()
    {
        var (servicio, canal, _) = Armar();
        canal.Falla = true;

        await servicio.ComunidadCreadaAsync(12);
        await servicio.ComunidadActualizadaAsync(12, ComunidadEfimera.EstadoArchivada, 0);
        await servicio.AvisosCambiaronAsync([1, 2]);
    }

    [Fact]
    public async Task El_cambio_de_cupo_pasa_tal_cual()
    {
        var (servicio, canal, _) = Armar();

        await servicio.ComunidadActualizadaAsync(12, ComunidadEfimera.EstadoActiva, 3);

        Assert.Equal((12, "ACTIVA", 3), Assert.Single(canal.Cambios));
    }

    private sealed class Canal : ICanalEnVivo
    {
        public bool Falla { get; set; }
        public List<int[]> Avisos { get; } = [];
        public List<int> Sedes { get; } = [];
        public List<(int, string, int)> Cambios { get; } = [];

        public Task AvisosCambiaronAsync(IReadOnlyCollection<int> usuarioIds, CancellationToken ct = default)
        {
            if (Falla) throw new InvalidOperationException("SignalR caído");
            Avisos.Add([.. usuarioIds]);
            return Task.CompletedTask;
        }

        public Task ComunidadesCambiaronAsync(int zonaId, CancellationToken ct = default)
        {
            if (Falla) throw new InvalidOperationException("SignalR caído");
            Sedes.Add(zonaId);
            return Task.CompletedTask;
        }

        public Task ComunidadActualizadaAsync(int comunidadId, string estado, int confirmados, CancellationToken ct = default)
        {
            if (Falla) throw new InvalidOperationException("SignalR caído");
            Cambios.Add((comunidadId, estado, confirmados));
            return Task.CompletedTask;
        }
    }
}
