using Breaku.Application.Notificaciones;
using Breaku.Application.Puentes;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Tests;

public class DespuesDeClasesTests
{
    private const int Lunes = 1;
    private const int Martes = 2;
    private const int Sabado = 6;

    private static Horario B(int dia, string ini, string fin, bool asiste = true) => new()
    {
        Materia = $"Clase {ini}", DiaSemana = dia,
        HoraInicio = TimeOnly.Parse(ini), HoraFin = TimeOnly.Parse(fin), Asiste = asiste
    };

    private static List<Puente> DespuesDeClases(params Horario[] bloques)
        => PuenteService.DetectarPuentes(1, bloques).Where(p => p.Tipo == Puente.TipoDespuesDeClases).ToList();

    [Fact]
    public void Va_desde_el_fin_de_la_ultima_clase_hasta_las_21()
    {
        var tramo = Assert.Single(DespuesDeClases(B(Lunes, "08:00", "09:30"), B(Lunes, "14:00", "18:00")));

        Assert.Equal(Lunes, tramo.DiaSemana);
        Assert.Equal(new TimeOnly(18, 0), tramo.HoraInicio);
        Assert.Equal(new TimeOnly(21, 0), tramo.HoraFin);
        Assert.Equal(1, tramo.UsuarioId);
    }

    [Theory]
    [InlineData("20:15", true)]
    [InlineData("20:16", false)]
    [InlineData("20:30", false)]
    [InlineData("21:00", false)]
    public void Necesita_45_minutos_antes_de_las_21(string fin, bool hayTramo)
        => Assert.Equal(hayTramo, DespuesDeClases(B(Lunes, "19:00", fin)).Count == 1);

    [Fact]
    public void No_cuenta_el_tiempo_antes_de_la_primera_clase()
        => Assert.Empty(PuenteService.DetectarPuentes(1, [B(Lunes, "14:00", "20:30")]));

    [Fact]
    public void Solo_los_dias_con_clases_tienen_tramo()
    {
        var tramos = DespuesDeClases(B(Lunes, "08:00", "10:00"), B(Martes, "08:00", "12:00"), B(Sabado, "08:00", "09:00"));

        Assert.Equal([Lunes, Martes, Sabado], tramos.Select(p => (int)p.DiaSemana).Order());
    }

    [Fact]
    public void Una_clase_a_la_que_no_asiste_no_retrasa_la_salida()
    {
        var tramo = Assert.Single(DespuesDeClases(B(Lunes, "08:00", "10:00"), B(Lunes, "17:00", "19:00", asiste: false)));

        Assert.Equal(new TimeOnly(10, 0), tramo.HoraInicio);
    }

    [Fact]
    public void Si_no_asiste_a_ninguna_clase_ese_dia_no_hay_tramo()
        => Assert.Empty(DespuesDeClases(B(Lunes, "08:00", "10:00", asiste: false)));

    [Fact]
    public void Con_clases_que_se_cruzan_toma_la_que_termina_mas_tarde()
    {
        var tramo = Assert.Single(DespuesDeClases(B(Lunes, "08:00", "12:00"), B(Lunes, "09:00", "10:00")));

        Assert.Equal(new TimeOnly(12, 0), tramo.HoraInicio);
    }

    [Fact]
    public void Los_puentes_entre_clases_siguen_siendo_entre_clases()
    {
        var puentes = PuenteService.DetectarPuentes(1, [B(Lunes, "08:00", "09:30"), B(Lunes, "11:00", "12:00")])
            .OrderBy(p => p.HoraInicio).ToList();

        Assert.Equal(2, puentes.Count);
        Assert.Equal(Puente.TipoEntreClases, puentes[0].Tipo);
        Assert.Equal(new TimeOnly(11, 0), puentes[0].HoraFin);
        Assert.Equal(Puente.TipoDespuesDeClases, puentes[1].Tipo);
    }

    [Fact]
    public void Una_actividad_despues_de_clases_invita_a_quien_ya_salio()
    {
        var actividad = new ReglasMatching.Franja(Lunes, new TimeOnly(18, 0), new TimeOnly(19, 0));
        var yaSalio = new ReglasMatching.PuenteDe(7, new ReglasMatching.Franja(Lunes, new TimeOnly(16, 0), new TimeOnly(21, 0)));
        var enClase = new ReglasMatching.PuenteDe(8, new ReglasMatching.Franja(Lunes, new TimeOnly(19, 45), new TimeOnly(21, 0)));

        var invitado = Assert.Single(ReglasComunidad.ElegirInvitados(actividad, 3, [yaSalio, enClase]));

        Assert.Equal(7, invitado.UsuarioId);
    }

    [Fact]
    public void Dos_estudiantes_que_salieron_coinciden_en_el_matching()
    {
        var mia = new ReglasMatching.Franja(Lunes, new TimeOnly(18, 0), new TimeOnly(21, 0));
        var suya = new ReglasMatching.PuenteDe(7, new ReglasMatching.Franja(Lunes, new TimeOnly(19, 0), new TimeOnly(21, 0)));

        var coincidencia = Assert.Single(ReglasMatching.Calcular(mia, [mia], [suya]));

        Assert.Equal(120, coincidencia.MinutosEnComun);
    }

    private static readonly DateTime LunesALasSeis = new(2026, 11, 2, 22, 0, 0, DateTimeKind.Utc); // 18:00 en Bolivia

    [Fact]
    public async Task El_aviso_dice_que_terminaron_las_clases()
    {
        var repo = new AvisosFalsos { PorAvisar = [Tramo(Puente.TipoDespuesDeClases, "18:00", "21:00")] };

        Assert.Equal(1, await new NotificacionService(repo).EnviarAvisosAsync(LunesALasSeis));

        var aviso = Assert.Single(repo.Guardadas);
        Assert.Equal(Notificacion.TipoDespuesDeClases, aviso.Tipo);
        Assert.Equal("¡Terminaste tus clases! Tienes 3 h libres, hasta las 21:00.", aviso.Mensaje);
        Assert.Equal(new TimeOnly(21, 0), aviso.PuenteHoraFin);
    }

    [Fact]
    public async Task Un_puente_entre_clases_conserva_su_aviso()
    {
        var repo = new AvisosFalsos { PorAvisar = [Tramo(Puente.TipoEntreClases, "18:00", "19:00")] };

        await new NotificacionService(repo).EnviarAvisosAsync(LunesALasSeis);

        var aviso = Assert.Single(repo.Guardadas);
        Assert.Equal(Notificacion.TipoPuenteDisponible, aviso.Tipo);
        Assert.StartsWith("¡Puente disponible!", aviso.Mensaje);
    }

    [Fact]
    public async Task Si_cambia_el_horario_se_apaga_el_aviso_que_quedo_viejo()
    {
        var viejo = new Notificacion
        {
            Id = 9, Tipo = Notificacion.TipoDespuesDeClases, PuenteFecha = new DateOnly(2026, 11, 2),
            PuenteHoraInicio = new TimeOnly(18, 0), PuenteHoraFin = new TimeOnly(21, 0)
        };
        var repo = new AvisosFalsos { Activas = [viejo] };

        await new NotificacionService(repo).DesactivarSinPuenteAsync(1, [Tramo(Puente.TipoDespuesDeClases, "19:00", "21:00")]);

        Assert.Equal([9L], repo.Desactivadas);
    }

    private static Puente Tramo(string tipo, string ini, string fin) => new()
    {
        UsuarioId = 1, DiaSemana = Lunes, HoraInicio = TimeOnly.Parse(ini), HoraFin = TimeOnly.Parse(fin), Tipo = tipo
    };

    private sealed class AvisosFalsos : INotificacionRepository
    {
        public List<Puente> PorAvisar { get; init; } = [];
        public List<Notificacion> Activas { get; init; } = [];
        public List<Notificacion> Guardadas { get; } = [];
        public List<long> Desactivadas { get; } = [];

        public Task<List<Puente>> ListarPuentesPorAvisarAsync(int diaSemana, DateOnly fecha, TimeOnly ahora, CancellationToken ct = default)
            => Task.FromResult(PorAvisar);

        public Task<bool> AgregarAsync(Notificacion notificacion, CancellationToken ct = default)
        {
            Guardadas.Add(notificacion);
            return Task.FromResult(true);
        }

        public Task<List<Notificacion>> ListarActivasDePuenteAsync(int usuarioId, CancellationToken ct = default) => Task.FromResult(Activas);

        public Task DesactivarAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default)
        {
            Desactivadas.AddRange(ids);
            return Task.CompletedTask;
        }

        public Task<List<Notificacion>> ListarActivasAsync(int usuarioId, int maximo, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> MarcarLeidaAsync(long id, int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool?> ObtenerPreferenciaAsync(int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task GuardarPreferenciaAsync(int usuarioId, bool activas, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
