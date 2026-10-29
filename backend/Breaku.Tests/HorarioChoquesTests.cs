using Breaku.Application.Puentes;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Tests;

public class UnionDeIntervalosTests
{
    private static ReglasHorario.Intervalo I(string ini, string fin) => new(TimeOnly.Parse(ini), TimeOnly.Parse(fin));

    [Fact]
    public void Sin_intervalos_devuelve_lista_vacia()
        => Assert.Empty(ReglasHorario.UnirIntervalos([]));

    [Fact]
    public void Intervalos_separados_se_conservan_ordenados()
    {
        var unidos = ReglasHorario.UnirIntervalos([I("14:15", "15:45"), I("08:00", "09:30")]);
        Assert.Equal([I("08:00", "09:30"), I("14:15", "15:45")], unidos);
    }

    [Fact]
    public void Intervalos_identicos_se_funden_en_uno()
        => Assert.Equal([I("14:15", "15:45")], ReglasHorario.UnirIntervalos([I("14:15", "15:45"), I("14:15", "15:45")]));

    [Fact]
    public void Intervalos_que_se_cruzan_se_unen_en_el_mas_largo()
        => Assert.Equal([I("14:15", "16:15")], ReglasHorario.UnirIntervalos([I("14:15", "15:45"), I("15:00", "16:15")]));

    [Fact]
    public void Un_intervalo_contenido_en_otro_no_agrega_nada()
        => Assert.Equal([I("08:00", "12:00")], ReglasHorario.UnirIntervalos([I("08:00", "12:00"), I("09:00", "10:00")]));

    [Fact]
    public void Intervalos_que_se_tocan_se_unen()
        => Assert.Equal([I("08:00", "11:00")], ReglasHorario.UnirIntervalos([I("08:00", "09:30"), I("09:30", "11:00")]));

    [Fact]
    public void Una_cadena_de_solapes_queda_en_un_solo_intervalo()
        => Assert.Equal([I("08:00", "12:00")], ReglasHorario.UnirIntervalos([I("10:00", "12:00"), I("08:00", "09:30"), I("09:00", "10:30")]));
}

public class DeteccionDeChoquesTests
{
    private static Horario B(int id, string materia, int dia, string ini, string fin, bool asiste = true) => new()
    {
        Id = id, Materia = materia, DiaSemana = dia,
        HoraInicio = TimeOnly.Parse(ini), HoraFin = TimeOnly.Parse(fin), Asiste = asiste
    };

    [Fact]
    public void Dos_materias_en_la_misma_franja_del_mismo_dia_chocan()
        => Assert.True(ReglasHorario.HayChoque(B(1, "MAT-133", 1, "14:15", "15:45"), B(2, "MAT-241", 1, "14:15", "15:45")));

    [Fact]
    public void Clases_seguidas_no_chocan()
        => Assert.False(ReglasHorario.HayChoque(B(1, "MAT-133", 1, "12:45", "14:15"), B(2, "MAT-241", 1, "14:15", "15:45")));

    [Fact]
    public void La_misma_franja_en_otro_dia_no_choca()
        => Assert.False(ReglasHorario.HayChoque(B(1, "MAT-133", 1, "14:15", "15:45"), B(2, "MAT-241", 2, "14:15", "15:45")));

    [Fact]
    public void Si_no_asiste_a_uno_no_hay_choque_pero_si_coincidencia()
    {
        var a = B(1, "MAT-133", 1, "14:15", "15:45");
        var b = B(2, "MAT-241", 1, "14:15", "15:45", asiste: false);

        Assert.False(ReglasHorario.HayChoque(a, b));
        Assert.Empty(ReglasHorario.Choques(a, [a, b]));
        Assert.Equal([b], ReglasHorario.Coincidencias(a, [a, b]));
    }

    [Fact]
    public void Un_bloque_al_que_no_asiste_nunca_tiene_choques()
    {
        var a = B(1, "MAT-133", 1, "14:15", "15:45", asiste: false);
        var b = B(2, "MAT-241", 1, "14:15", "15:45");

        Assert.Empty(ReglasHorario.Choques(a, [a, b]));
    }

    [Fact]
    public void Un_bloque_no_choca_consigo_mismo()
    {
        var a = B(1, "MAT-133", 1, "14:15", "15:45");

        Assert.Empty(ReglasHorario.Choques(a, [a]));
    }

    [Fact]
    public void Un_bloque_sin_guardar_choca_con_los_existentes_del_dia()
    {
        var nuevo = B(0, "MAT-241", 1, "14:15", "15:45");
        var existente = B(1, "MAT-133", 1, "14:15", "15:45");
        var otroDia = B(2, "FIS-101", 2, "14:15", "15:45");

        Assert.Equal([existente], ReglasHorario.Choques(nuevo, [existente, otroDia]));
    }
}

public class HuecosLibresConAsistenciaTests
{
    private const int Lunes = 1, Martes = 2, Miercoles = 3, Jueves = 4;

    private static Horario B(int id, string materia, int dia, string ini, string fin, bool asiste = true) => new()
    {
        Id = id, Materia = materia, DiaSemana = dia,
        HoraInicio = TimeOnly.Parse(ini), HoraFin = TimeOnly.Parse(fin), Asiste = asiste
    };

    private static List<Horario> ClasesFijas() =>
    [
        B(1, "Clase de la mañana", Lunes, "08:00", "09:30"), B(2, "Clase de la mañana", Martes, "08:00", "09:30"),
        B(3, "Clase de la mañana", Miercoles, "08:00", "09:30"), B(4, "Clase de la mañana", Jueves, "08:00", "09:30"),
        B(5, "Clase de la tarde", Lunes, "17:00", "18:30"), B(6, "Clase de la tarde", Martes, "17:00", "18:30"),
        B(7, "Clase de la tarde", Miercoles, "17:00", "18:30"), B(8, "Clase de la tarde", Jueves, "17:00", "18:30")
    ];

    private static List<(int Dia, string Inicio, string Fin)> Huecos(IEnumerable<Horario> bloques)
        => PuenteService.DetectarPuentes(1, bloques)
            .OrderBy(p => p.DiaSemana).ThenBy(p => p.HoraInicio)
            .Select(p => ((int)p.DiaSemana, p.HoraInicio.ToString("HH:mm"), p.HoraFin.ToString("HH:mm"))).ToList();

    [Fact]
    public void Caso_una_sola_clase_la_otra_no_ocupa_tiempo()
    {
        var bloques = ClasesFijas();
        for (var dia = Lunes; dia <= Jueves; dia++)
        {
            bloques.Add(B(100 + dia, "MAT-133", dia, "14:15", "15:45"));
            bloques.Add(B(200 + dia, "MAT-241", dia, "14:15", "16:15", asiste: false));
        }

        var huecos = Huecos(bloques);

        Assert.Equal(8, huecos.Count);
        Assert.Contains((Lunes, "09:30", "14:15"), huecos);
        Assert.Contains((Lunes, "15:45", "17:00"), huecos);
        Assert.Contains((Jueves, "15:45", "17:00"), huecos);
    }

    [Fact]
    public void Caso_alternar_por_dia_cada_dia_usa_la_clase_a_la_que_asiste()
    {
        var bloques = ClasesFijas();
        foreach (var dia in new[] { Lunes, Martes })
        {
            bloques.Add(B(100 + dia, "MAT-133", dia, "14:15", "15:45"));
            bloques.Add(B(200 + dia, "MAT-241", dia, "14:15", "16:15", asiste: false));
        }
        foreach (var dia in new[] { Miercoles, Jueves })
        {
            bloques.Add(B(100 + dia, "MAT-133", dia, "14:15", "15:45", asiste: false));
            bloques.Add(B(200 + dia, "MAT-241", dia, "14:15", "16:15"));
        }

        var huecos = Huecos(bloques);

        Assert.Contains((Lunes, "15:45", "17:00"), huecos);
        Assert.Contains((Martes, "15:45", "17:00"), huecos);
        Assert.Contains((Miercoles, "16:15", "17:00"), huecos);
        Assert.Contains((Jueves, "16:15", "17:00"), huecos);
        Assert.DoesNotContain((Miercoles, "15:45", "17:00"), huecos);
    }

    [Fact]
    public void Caso_ambas_marcadas_el_tiempo_ocupado_es_la_union_y_no_hay_huecos_negativos_ni_repetidos()
    {
        var bloques = ClasesFijas();
        bloques.Add(B(101, "MAT-133", Lunes, "14:15", "15:45"));
        bloques.Add(B(201, "MAT-241", Lunes, "14:15", "16:15"));

        var huecos = Huecos(bloques).Where(h => h.Dia == Lunes).ToList();

        Assert.Equal([(Lunes, "09:30", "14:15"), (Lunes, "16:15", "17:00")], huecos);
    }

    [Fact]
    public void Dos_clases_identicas_marcadas_dejan_los_mismos_huecos_que_una_sola()
    {
        var conUna = ClasesFijas();
        conUna.Add(B(101, "MAT-133", Lunes, "14:15", "15:45"));
        var conAmbas = ClasesFijas();
        conAmbas.Add(B(101, "MAT-133", Lunes, "14:15", "15:45"));
        conAmbas.Add(B(201, "MAT-241", Lunes, "14:15", "15:45"));

        Assert.Equal(Huecos(conUna), Huecos(conAmbas));
    }

    [Fact]
    public void Si_no_asiste_a_ninguna_de_las_dos_el_hueco_se_junta_en_uno_solo()
    {
        var bloques = ClasesFijas();
        bloques.Add(B(101, "MAT-133", Lunes, "14:15", "15:45", asiste: false));
        bloques.Add(B(201, "MAT-241", Lunes, "14:15", "15:45", asiste: false));

        var huecos = Huecos(bloques).Where(h => h.Dia == Lunes).ToList();

        Assert.Equal([(Lunes, "09:30", "17:00")], huecos);
    }

    [Fact]
    public void Una_clase_que_contiene_a_otra_no_genera_un_hueco_en_medio()
    {
        var bloques = new List<Horario>
        {
            B(1, "Clase larga", Lunes, "08:00", "12:00"),
            B(2, "Clase corta dentro", Lunes, "09:00", "10:00"),
            B(3, "Clase de la tarde", Lunes, "14:00", "15:30")
        };

        Assert.Equal([(Lunes, "12:00", "14:00")], Huecos(bloques));
    }
}
