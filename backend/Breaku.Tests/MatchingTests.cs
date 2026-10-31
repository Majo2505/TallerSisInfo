using Breaku.Domain.Rules;
using static Breaku.Domain.Rules.ReglasMatching;

namespace Breaku.Tests;

public class CruceDeFranjasTests
{
    private static Franja F(int dia, string ini, string fin) => new(dia, TimeOnly.Parse(ini), TimeOnly.Parse(fin));

    [Fact]
    public void Franjas_que_se_cruzan_comparten_el_tramo_del_medio()
        => Assert.Equal(F(1, "10:30", "12:00"), Cruce(F(1, "10:00", "12:00"), F(1, "10:30", "13:00")));

    [Fact]
    public void El_cruce_es_el_mismo_en_ambos_sentidos()
        => Assert.Equal(Cruce(F(1, "10:00", "12:00"), F(1, "10:30", "13:00")), Cruce(F(1, "10:30", "13:00"), F(1, "10:00", "12:00")));

    [Fact]
    public void Una_franja_contenida_en_otra_comparte_la_mas_corta()
        => Assert.Equal(F(2, "09:00", "10:00"), Cruce(F(2, "08:00", "12:00"), F(2, "09:00", "10:00")));

    [Fact]
    public void Dias_distintos_no_se_cruzan()
        => Assert.Null(Cruce(F(1, "10:00", "12:00"), F(2, "10:00", "12:00")));

    [Fact]
    public void Franjas_que_solo_se_tocan_no_se_cruzan()
        => Assert.Null(Cruce(F(1, "09:00", "10:00"), F(1, "10:00", "11:00")));

    [Fact]
    public void Franjas_separadas_no_se_cruzan()
        => Assert.Null(Cruce(F(1, "08:00", "09:00"), F(1, "11:00", "12:00")));

    [Fact]
    public void Treinta_minutos_exactos_en_comun_cuentan()
        => Assert.Equal(F(1, "10:30", "11:00"), Cruce(F(1, "10:00", "11:00"), F(1, "10:30", "12:00")));

    [Fact]
    public void Menos_de_treinta_minutos_en_comun_no_cuentan()
        => Assert.Null(Cruce(F(1, "10:00", "11:00"), F(1, "10:31", "12:00")));
}

public class PorcentajeDeCompatibilidadTests
{
    private static Franja F(string ini, string fin) => new(1, TimeOnly.Parse(ini), TimeOnly.Parse(fin));

    [Fact]
    public void Cubrir_todo_mi_puente_es_cien_por_ciento()
        => Assert.Equal(100, Porcentaje(F("10:00", "11:30"), 90));

    [Fact]
    public void Cubrir_la_mitad_es_cincuenta_por_ciento()
        => Assert.Equal(50, Porcentaje(F("10:00", "12:00"), 60));

    [Fact]
    public void Se_redondea_al_entero_mas_cercano()
        => Assert.Equal(67, Porcentaje(F("10:00", "11:30"), 60));
}

public class OrdenPorCompatibilidadTests
{
    private static Franja F(int dia, string ini, string fin) => new(dia, TimeOnly.Parse(ini), TimeOnly.Parse(fin));
    private static PuenteDe De(int usuario, int dia, string ini, string fin) => new(usuario, F(dia, ini, fin));

    private static readonly Franja Mia = F(1, "09:30", "11:00");

    [Fact]
    public void Primero_quien_comparte_mas_minutos()
    {
        var resultado = Calcular(Mia, [Mia], [
            De(7, 1, "10:00", "11:30"),   // 60 min
            De(8, 1, "09:30", "11:00"),   // 90 min
            De(9, 1, "09:00", "10:30"),   // 60 min
        ]);

        Assert.Equal(8, resultado[0].UsuarioId);
        Assert.Equal(90, resultado[0].MinutosEnComun);
        Assert.Equal(100, resultado[0].Porcentaje);
    }

    [Fact]
    public void En_empate_gana_quien_coincide_en_mas_franjas_de_la_semana()
    {
        var miMiercoles = F(3, "10:00", "12:00");
        var resultado = Calcular(Mia, [Mia, miMiercoles], [
            De(7, 1, "10:00", "11:30"),                                // 60 min el lunes, nada más
            De(9, 1, "09:00", "10:30"), De(9, 3, "10:30", "12:30"),     // 60 min el lunes y también el miércoles
        ]);

        Assert.Equal([9, 7], resultado.Select(c => c.UsuarioId));
        Assert.Equal(2, resultado[0].FranjasCompartidas);
        Assert.Equal(1, resultado[1].FranjasCompartidas);
    }

    [Fact]
    public void Quien_no_se_cruza_o_comparte_menos_del_minimo_no_aparece()
    {
        var resultado = Calcular(Mia, [Mia], [
            De(7, 2, "09:30", "11:00"),   // otro día
            De(8, 1, "10:45", "12:00"),   // 15 min
            De(9, 1, "11:00", "12:00"),   // solo se toca
        ]);

        Assert.Empty(resultado);
    }

    [Fact]
    public void Con_dos_puentes_que_tocan_el_mio_cuenta_el_tramo_mas_largo()
    {
        var mia = F(1, "08:00", "14:00");
        var resultado = Calcular(mia, [mia], [De(7, 1, "08:30", "09:15"), De(7, 1, "10:00", "12:00")]);

        var unico = Assert.Single(resultado);
        Assert.Equal(F(1, "10:00", "12:00"), unico.EnComun);
        Assert.Equal(120, unico.MinutosEnComun);
        Assert.Equal(1, unico.FranjasCompartidas);
    }

    [Fact]
    public void En_empate_total_el_orden_es_estable()
    {
        var resultado = Calcular(Mia, [Mia], [De(9, 1, "09:30", "11:00"), De(4, 1, "09:30", "11:00")]);
        Assert.Equal([4, 9], resultado.Select(c => c.UsuarioId));
    }

    [Fact]
    public void Sin_companeros_no_hay_resultados()
        => Assert.Empty(Calcular(Mia, [Mia], []));
}
