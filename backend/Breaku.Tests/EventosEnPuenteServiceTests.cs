using Breaku.Application.Eventos;
using Breaku.Application.Lugares;
using Breaku.Application.Puentes;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Tests;

public sealed class EventosEnPuenteServiceTests
{
    private const int UsuarioId = 17;
    private static readonly DateTimeOffset Ahora = new(2026, 11, 16, 13, 0, 0, TimeSpan.Zero);

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }

    private sealed class Eventos : IEventoRepository
    {
        public List<EventoParaPuente> Publicados { get; set; } = [];
        public int VecesListado { get; private set; }
        public DateTime DesdeUtc { get; private set; }
        public DateTime HastaUtc { get; private set; }

        public Task<List<EventoParaPuente>> ListarPublicadosHoyAsync(
            DateTime desdeUtc, DateTime hastaUtc, DateTime ahoraUtc, CancellationToken ct = default)
        {
            VecesListado++;
            DesdeUtc = desdeUtc;
            HastaUtc = hastaUtc;
            return Task.FromResult(Publicados);
        }

        public Task<List<Lugar>> ListarLugaresDisponiblesAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Lugar?> ObtenerLugarAsync(int lugarId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> CrearAsync(Evento evento, byte[]? imagen, string? tipoMime, bool aplicarLimiteDiario,
            DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<ReglasEvento.CandidatoDuplicado>> BuscarCandidatosDuplicadoAsync(
            DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<EventoListado>> ListarMiosAsync(int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Evento?> ObtenerMioAsync(int eventoId, int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task GuardarCambiosAsync(Evento evento, byte[]? imagen, string? tipoMime, bool quitarImagen = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<FotoEvento?> ObtenerImagenAsync(int eventoId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<EventoListado>> ListarPublicadosAsync(DateTime ahoraUtc, DateTime hastaUtc, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<EventoListado?> ObtenerPublicadoAsync(int eventoId, DateTime ahoraUtc, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<List<EventoListado>> ListarPendientesAsync(DateTime ahoraUtc, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<EventoRechazado>> ListarRechazadosAsync(int maximo, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ResultadoModeracionEvento> ModerarAsync(ModeracionEvento moderacion, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private sealed class Lugares : ILugarRepository
    {
        public Zona? Zona { get; set; } = new()
        {
            Id = 3,
            Nombre = "Campus",
            Latitud = -17.37m,
            Longitud = -66.16m,
            RadioMaxM = 2_000
        };

        public Task<Zona?> ObtenerZonaDelUsuarioAsync(int usuarioId, CancellationToken ct = default)
            => Task.FromResult(Zona);

        public Task<List<Lugar>> ListarActivosAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Dictionary<int, (int MeGusta, int NoMeSirve)>> ContarVotosAsync(
            IReadOnlyCollection<int> lugarIds, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Dictionary<int, List<int>>> ListarEtiquetasDeLugaresAsync(
            IReadOnlyCollection<int> lugarIds, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<ReglasAfinidad.VotoLugar>> ListarVotosDelUsuarioAsync(
            int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Dictionary<int, string>> ListarNombresEtiquetasAsync(CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private sealed class Puentes : IPuenteRepository
    {
        public List<Puente> Items { get; set; } = [];

        public Task<List<Puente>> ListarPorUsuarioAsync(int usuarioId, CancellationToken ct = default)
            => Task.FromResult(Items);

        public Task ReemplazarAsync(int usuarioId, IReadOnlyList<Puente> nuevos, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private static DateTime UtcLocal(int dia, int hora, int minuto)
        => new DateTimeOffset(2026, 11, dia, hora, minuto, 0, TimeSpan.FromHours(-4)).UtcDateTime;

    [Fact]
    public async Task Devuelve_eventos_del_dia_que_caben_en_puentes_y_usa_la_zona_del_estudiante()
    {
        var eventos = new Eventos
        {
            Publicados =
            [
                new EventoParaPuente(
                    new Evento
                    {
                        Id = 2,
                        Titulo = "Charla",
                        LugarId = 5,
                        Estado = Evento.EstadoPublicado,
                        Asistencia = ReglasEvento.AsistenciaFija,
                        Inicio = UtcLocal(16, 10, 30),
                        Fin = UtcLocal(16, 11, 10)
                    },
                    new Lugar { Id = 5, Nombre = "Biblioteca", Latitud = -17.37m, Longitud = -66.16m })
            ]
        };
        var puentes = new Puentes
        {
            Items =
            [
                new Puente
                {
                    UsuarioId = UsuarioId,
                    DiaSemana = 1,
                    HoraInicio = new TimeOnly(10, 0),
                    HoraFin = new TimeOnly(11, 30),
                    Tipo = Puente.TipoEntreClases
                }
            ]
        };
        var servicio = new EventosEnPuenteService(eventos, new Lugares(), puentes, new RelojFijo(Ahora));

        var recomendados = await servicio.ListarAsync(UsuarioId);

        var evento = Assert.Single(recomendados);
        Assert.Equal("10:30", evento.Inicio);
        Assert.Equal("11:10", evento.Fin);
        Assert.Equal("Biblioteca", evento.Lugar);
        Assert.Contains("Hay que llegar a la hora", evento.Explicacion);
        Assert.Equal(new DateTime(2026, 11, 16, 4, 0, 0, DateTimeKind.Utc), eventos.DesdeUtc);
        Assert.Equal(new DateTime(2026, 11, 17, 4, 0, 0, DateTimeKind.Utc), eventos.HastaUtc);
    }

    [Fact]
    public async Task Sin_puentes_de_hoy_o_sin_zona_devuelve_lista_vacia_sin_consultar_eventos()
    {
        var eventos = new Eventos();
        var lugares = new Lugares();
        var puentes = new Puentes();
        var servicio = new EventosEnPuenteService(eventos, lugares, puentes, new RelojFijo(Ahora));

        Assert.Empty(await servicio.ListarAsync(UsuarioId));
        Assert.Equal(0, eventos.VecesListado);

        puentes.Items =
        [
            new Puente
            {
                DiaSemana = 1,
                HoraInicio = new TimeOnly(10, 0),
                HoraFin = new TimeOnly(11, 0)
            }
        ];
        lugares.Zona = null;

        Assert.Empty(await servicio.ListarAsync(UsuarioId));
        Assert.Equal(0, eventos.VecesListado);
    }
}
