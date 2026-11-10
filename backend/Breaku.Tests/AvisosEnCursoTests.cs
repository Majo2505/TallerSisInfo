using Breaku.Application.Notificaciones;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Tests;

/// <summary>
/// HU4: el aviso llega mientras el puente está en curso, no solo en su primer minuto. Antes, si ningún servidor estaba
/// prendido justo al empezar el puente, el aviso no se creaba nunca.
/// </summary>
public class AvisosEnCursoTests
{
    private static TimeOnly H(string hora) => TimeOnly.Parse(hora);

    // Lunes 2 de noviembre de 2026; Bolivia es UTC-4.
    private static DateTime LunesBolivia(int hora, int minuto) => new(2026, 11, 2, hora + 4, minuto, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("10:00", true)]
    [InlineData("10:00:59", true)]
    [InlineData("10:35", true)]
    [InlineData("11:20", true)]
    [InlineData("11:21", false)]
    [InlineData("11:30", false)]
    [InlineData("09:59", false)]
    public void Se_avisa_mientras_el_puente_esta_en_curso_y_le_quedan_10_minutos(string ahora, bool avisa)
        => Assert.Equal(avisa, ReglasNotificacion.DebeAvisarse(H("10:00"), H("11:30"), H(ahora)));

    [Fact]
    public void A_tiempo_cuenta_el_puente_completo_y_tarde_solo_lo_que_queda()
    {
        Assert.Equal(H("10:00"), ReglasNotificacion.DesdeCuandoContar(H("10:00"), H("10:00:40")));
        Assert.Equal(H("10:35"), ReglasNotificacion.DesdeCuandoContar(H("10:00"), H("10:35:50")));
    }

    [Fact]
    public async Task Un_aviso_atrasado_dice_cuanto_queda()
    {
        var repo = new Avisos { PorAvisar = [Tramo(Puente.TipoEntreClases, "10:00", "12:00")] };

        Assert.Equal(1, await new NotificacionService(repo).EnviarAvisosAsync(LunesBolivia(10, 30)));

        var aviso = Assert.Single(repo.Guardadas);
        Assert.Equal(Notificacion.TipoPuenteDisponible, aviso.Tipo);
        Assert.Equal("¡Estás en un puente! Te quedan 1 h 30 min libres, hasta las 12:00.", aviso.Mensaje);
        Assert.Equal(H("10:00"), aviso.PuenteHoraInicio);
    }

    [Fact]
    public async Task Un_aviso_a_tiempo_sigue_igual_que_antes()
    {
        var repo = new Avisos { PorAvisar = [Tramo(Puente.TipoEntreClases, "10:00", "12:00")] };

        await new NotificacionService(repo).EnviarAvisosAsync(LunesBolivia(10, 0));

        Assert.Equal("¡Puente disponible! Tienes 2 h libres, de 10:00 a 12:00.", Assert.Single(repo.Guardadas).Mensaje);
    }

    [Fact]
    public async Task Despues_de_clases_atrasado_cuenta_hasta_las_21()
    {
        var repo = new Avisos { PorAvisar = [Tramo(Puente.TipoDespuesDeClases, "16:00", "21:00")] };

        await new NotificacionService(repo).EnviarAvisosAsync(LunesBolivia(19, 30));

        var aviso = Assert.Single(repo.Guardadas);
        Assert.Equal(Notificacion.TipoDespuesDeClases, aviso.Tipo);
        Assert.Equal("¡Terminaste tus clases! Tienes 1 h 30 min libres, hasta las 21:00.", aviso.Mensaje);
    }

    [Fact]
    public async Task Si_ya_casi_termina_no_se_avisa()
    {
        var repo = new Avisos { PorAvisar = [Tramo(Puente.TipoEntreClases, "10:00", "11:00")] };

        Assert.Equal(0, await new NotificacionService(repo).EnviarAvisosAsync(LunesBolivia(10, 55)));
        Assert.Empty(repo.Guardadas);
    }

    [Fact]
    public async Task El_domingo_no_hay_avisos()
    {
        var repo = new Avisos { PorAvisar = [Tramo(Puente.TipoEntreClases, "10:00", "12:00")] };

        Assert.Equal(0, await new NotificacionService(repo).EnviarAvisosAsync(new DateTime(2026, 11, 8, 14, 30, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public async Task Se_le_pregunta_al_repositorio_por_la_hora_de_Bolivia()
    {
        var repo = new Avisos();

        await new NotificacionService(repo).EnviarAvisosAsync(LunesBolivia(10, 30));

        Assert.Equal((1, new DateOnly(2026, 11, 2), H("10:30")), repo.Consulta);
    }

    private static Puente Tramo(string tipo, string ini, string fin) => new()
    {
        UsuarioId = 1, DiaSemana = 1, HoraInicio = H(ini), HoraFin = H(fin), Tipo = tipo
    };

    private sealed class Avisos : INotificacionRepository
    {
        public List<Puente> PorAvisar { get; init; } = [];
        public List<Notificacion> Guardadas { get; } = [];
        public (int Dia, DateOnly Fecha, TimeOnly Ahora)? Consulta { get; private set; }

        public Task<List<Puente>> ListarPuentesPorAvisarAsync(int diaSemana, DateOnly fecha, TimeOnly ahora, CancellationToken ct = default)
        {
            Consulta = (diaSemana, fecha, ahora);
            return Task.FromResult(PorAvisar);
        }

        public Task<bool> AgregarAsync(Notificacion notificacion, CancellationToken ct = default)
        {
            Guardadas.Add(notificacion);
            return Task.FromResult(true);
        }

        public Task<List<Notificacion>> ListarActivasAsync(int usuarioId, int maximo, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<Notificacion>> ListarActivasDePuenteAsync(int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DesactivarAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> MarcarLeidaAsync(long id, int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool?> ObtenerPreferenciaAsync(int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task GuardarPreferenciaAsync(int usuarioId, bool activas, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
