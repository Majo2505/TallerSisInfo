using Breaku.Application.Eventos;
using Breaku.Application.Puentes;
using Breaku.Domain;

namespace Breaku.Tests;

public sealed class EventosServiceTests
{
    private static readonly DateTime Ahora = new(2026, 2, 1, 20, 0, 0, DateTimeKind.Utc);

    private static EventoUniversitario Evento(long id, DateTime inicioUtc, int minutos) => new()
    {
        Id = id,
        Titulo = "Charla",
        Descripcion = "Charla en la universidad",
        Inicio = inicioUtc,
        Fin = inicioUtc.AddMinutes(minutos),
        FuenteUrl = $"https://example.test/{id}"
    };

    private static Puente Puente(int dia, string desde, string hasta) => new()
    {
        DiaSemana = (short)dia,
        HoraInicio = TimeOnly.Parse(desde),
        HoraFin = TimeOnly.Parse(hasta)
    };

    private static readonly DateTime LunesOchoLocal = new(2026, 2, 2, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Recomienda_el_evento_que_cabe_en_un_puente()
    {
        var servicio = CrearServicio([Evento(1, LunesOchoLocal, 40)], Puente(1, "08:00", "09:00"));

        var resultado = await servicio.ListarRecomendadosAsync(1);

        var evento = Assert.Single(resultado);
        Assert.Equal("2026-02-02", evento.Fecha);
        Assert.Equal("08:00", evento.Inicio);
        Assert.Equal("08:40", evento.Fin);
    }

    [Fact]
    public async Task Recomienda_el_evento_que_dura_justo_lo_que_el_puente()
    {
        var servicio = CrearServicio([Evento(1, LunesOchoLocal, 30)], Puente(1, "08:00", "08:30"));

        Assert.Single(await servicio.ListarRecomendadosAsync(1));
    }

    [Fact]
    public async Task No_recomienda_el_evento_de_40_minutos_si_el_puente_dura_30()
    {
        var servicio = CrearServicio([Evento(1, LunesOchoLocal, 40)], Puente(1, "08:00", "08:30"));

        Assert.Empty(await servicio.ListarRecomendadosAsync(1));
    }

    [Fact]
    public async Task No_recomienda_el_evento_que_empieza_antes_o_termina_despues_del_puente()
    {
        var servicio = CrearServicio(
            [Evento(1, LunesOchoLocal.AddMinutes(-15), 40), Evento(2, LunesOchoLocal.AddMinutes(30), 40)],
            Puente(1, "08:00", "09:00"));

        Assert.Empty(await servicio.ListarRecomendadosAsync(1));
    }

    [Fact]
    public async Task No_recomienda_el_evento_de_un_dia_en_que_no_hay_puente()
    {
        var servicio = CrearServicio([Evento(1, LunesOchoLocal, 40)], Puente(2, "08:00", "09:00"));

        Assert.Empty(await servicio.ListarRecomendadosAsync(1));
    }

    [Fact]
    public async Task Sin_puentes_no_recomienda_nada()
    {
        var servicio = CrearServicio([Evento(1, LunesOchoLocal, 40)]);

        Assert.Empty(await servicio.ListarRecomendadosAsync(1));
    }

    [Fact]
    public async Task Recomienda_solo_los_que_caben_y_en_orden_de_fecha()
    {
        var servicio = CrearServicio(
            [Evento(2, LunesOchoLocal.AddDays(7), 30), Evento(1, LunesOchoLocal, 30), Evento(3, LunesOchoLocal, 90)],
            Puente(1, "08:00", "09:00"));

        var resultado = await servicio.ListarRecomendadosAsync(1);

        Assert.Equal([1L, 2L], resultado.Select(e => e.Id));
    }

    [Fact]
    public async Task No_recomienda_eventos_que_ya_terminaron()
    {
        var pasado = Evento(1, new DateTime(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc), 30);
        var servicio = CrearServicio([pasado], Puente(1, "08:00", "09:00"));

        Assert.Empty(await servicio.ListarRecomendadosAsync(1));
    }

    private static EventosService CrearServicio(IReadOnlyList<EventoUniversitario> eventos, params Puente[] puentes)
        => new(new EventosRepositoryStub(eventos), new PuentesRepositoryStub(puentes), new EventosPublicosClientStub(),
            new FixedTimeProvider(Ahora));

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private sealed class EventosPublicosClientStub : IEventosPublicosClient
    {
        public Task<IReadOnlyList<EventoImportado>> ObtenerAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<EventoImportado>>([]);
    }

    private sealed class EventosRepositoryStub(IReadOnlyList<EventoUniversitario> eventos) : IEventosRepository
    {
        public Task<List<EventoUniversitario>> ListarProximosAsync(DateTime ahoraUtc, CancellationToken ct = default)
            => Task.FromResult(eventos.Where(e => e.Fin > ahoraUtc).OrderBy(e => e.Inicio).ToList());

        public Task UpsertAsync(IReadOnlyList<EventoImportado> eventos, DateTime actualizadoEn, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class PuentesRepositoryStub(IReadOnlyList<Puente> puentes) : IPuenteRepository
    {
        public Task<List<Puente>> ListarPorUsuarioAsync(int usuarioId, CancellationToken ct = default)
            => Task.FromResult(puentes.ToList());

        public Task ReemplazarAsync(int usuarioId, IReadOnlyList<Puente> nuevos, CancellationToken ct = default)
            => throw new NotImplementedException();
    }
}
