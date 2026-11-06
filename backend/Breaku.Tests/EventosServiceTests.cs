using Breaku.Application.Eventos;
using Breaku.Application.Horarios;
using Breaku.Application.Puentes;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Tests;

public sealed class EventosServiceTests
{
    [Fact]
    public async Task RecomiendaEventoAntesDeLaSiguienteClaseAunqueSeaElDiaSiguiente()
    {
        var ahora = new DateTime(2026, 2, 1, 20, 0, 0, DateTimeKind.Utc);
        var eventos = new[]
        {
            new EventoUniversitario
            {
                Id = 1,
                Titulo = "Feria",
                Descripcion = "Feria universitaria",
                Inicio = new DateTime(2026, 2, 2, 12, 0, 0, DateTimeKind.Utc),
                Fin = new DateTime(2026, 2, 2, 13, 0, 0, DateTimeKind.Utc),
                FuenteUrl = "https://example.test/feria"
            }
        };
        var service = CrearServicio(
            ahora,
            new Horario { DiaSemana = 1, HoraInicio = new TimeOnly(10, 0), Asiste = true },
            eventos);

        var resultado = await service.ListarRecomendadosAsync(1);

        Assert.Single(resultado);
        Assert.Equal("08:00", resultado[0].Inicio);
    }

    [Fact]
    public async Task ExcluyeEventoQueSeCruzaConUnPuenteEnHoraLocal()
    {
        var ahora = new DateTime(2026, 2, 2, 11, 0, 0, DateTimeKind.Utc);
        var eventos = new[]
        {
            new EventoUniversitario
            {
                Id = 1,
                Titulo = "Feria",
                Descripcion = "Feria universitaria",
                Inicio = new DateTime(2026, 2, 2, 12, 0, 0, DateTimeKind.Utc),
                Fin = new DateTime(2026, 2, 2, 13, 0, 0, DateTimeKind.Utc),
                FuenteUrl = "https://example.test/feria"
            }
        };
        var service = CrearServicio(
            ahora,
            new Horario { DiaSemana = 1, HoraInicio = new TimeOnly(10, 0), Asiste = true },
            eventos,
            new Puente { DiaSemana = 1, HoraInicio = new TimeOnly(7, 30), HoraFin = new TimeOnly(8, 30) });

        var resultado = await service.ListarRecomendadosAsync(1);

        Assert.Empty(resultado);
    }

    private static EventosService CrearServicio(
        DateTime ahora,
        Horario horario,
        IReadOnlyList<EventoUniversitario> eventos,
        params Puente[] puentes)
        => new(
            new EventosRepositoryStub(eventos),
            new HorariosRepositoryStub(horario),
            new PuentesRepositoryStub(puentes),
            new EventosPublicosClientStub(),
            new FixedTimeProvider(ahora));

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
        public Task<List<EventoUniversitario>> ListarVigentesAsync(DateTime ahoraUtc, DateTime hastaUtc, CancellationToken ct = default)
            => Task.FromResult(eventos.Where(e => e.Inicio < hastaUtc && e.Fin > ahoraUtc).ToList());

        public Task UpsertAsync(IReadOnlyList<EventoImportado> eventos, DateTime actualizadoEn, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class HorariosRepositoryStub(Horario horario) : IHorarioRepository
    {
        public Task<List<Horario>> ListarPorUsuarioAsync(int usuarioId, CancellationToken ct = default)
            => Task.FromResult(new List<Horario> { horario });

        public Task<Horario?> ObtenerAsync(int id, int usuarioId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AgregarAsync(Horario horario, CancellationToken ct = default) => throw new NotImplementedException();
        public void Eliminar(Horario horario) => throw new NotImplementedException();
        public Task GuardarCambiosAsync(CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class PuentesRepositoryStub(IReadOnlyList<Puente> puentes) : IPuenteRepository
    {
        public Task<List<Puente>> ListarPorUsuarioAsync(int usuarioId, CancellationToken ct = default)
            => Task.FromResult(puentes.ToList());

        public Task ReemplazarAsync(int usuarioId, IReadOnlyList<Puente> nuevos, CancellationToken ct = default)
            => throw new NotImplementedException();
    }
}
