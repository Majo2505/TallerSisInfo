using Breaku.Infrastructure.Services;

namespace Breaku.Tests;

public sealed class LectorEventosInstagramTests
{
    private const string Perfil = "https://example.test/perfil";
    private static readonly DateTime Ahora = new(2026, 11, 5, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Interpreta_la_fecha_y_el_rango_en_hora_de_Bolivia()
    {
        var evento = LectorEventosInstagram.Interpretar("Charla de IA\nMiércoles 12 de noviembre 18:30 a 19:15 en el auditorio", Perfil, Ahora);

        Assert.NotNull(evento);
        Assert.Equal("Charla de IA", evento.Titulo);
        Assert.Equal(new DateTime(2026, 11, 12, 22, 30, 0, DateTimeKind.Utc), evento.Inicio);
        Assert.Equal(new DateTime(2026, 11, 12, 23, 15, 0, DateTimeKind.Utc), evento.Fin);
    }

    [Fact]
    public void Sin_hora_de_fin_dura_dos_horas()
    {
        var evento = LectorEventosInstagram.Interpretar("Feria\n12/11 18:00", Perfil, Ahora);

        Assert.NotNull(evento);
        Assert.Equal(TimeSpan.FromHours(2), evento.Fin - evento.Inicio);
    }

    [Theory]
    [InlineData("Feria\n31/02 18:00")]
    [InlineData("Feria\n12/13 18:00")]
    [InlineData("Feria\n12/11 25:00")]
    [InlineData("Feria sin fecha ni hora")]
    public void Ignora_los_textos_con_fecha_u_hora_imposibles(string caption)
        => Assert.Null(LectorEventosInstagram.Interpretar(caption, Perfil, Ahora));

    [Fact]
    public void Si_el_fin_es_anterior_al_inicio_usa_la_duracion_por_defecto()
    {
        var evento = LectorEventosInstagram.Interpretar("Noche\n12/11 22:00 a 01:00", Perfil, Ahora);

        Assert.NotNull(evento);
        Assert.Equal(TimeSpan.FromHours(2), evento.Fin - evento.Inicio);
    }

    [Fact]
    public void Una_fecha_sin_anio_que_ya_paso_hace_mucho_es_del_anio_siguiente()
    {
        var diciembre = new DateTime(2026, 12, 20, 15, 0, 0, DateTimeKind.Utc);

        var evento = LectorEventosInstagram.Interpretar("Bienvenida\n5 de enero 10:00 a 11:00", Perfil, diciembre);

        Assert.NotNull(evento);
        Assert.Equal(new DateTime(2027, 1, 5, 14, 0, 0, DateTimeKind.Utc), evento.Inicio);
    }

    [Fact]
    public void El_identificador_no_cambia_si_solo_cambia_la_descripcion()
    {
        var a = LectorEventosInstagram.Interpretar("Feria\n12/11 18:00\nEntrada libre", Perfil, Ahora);
        var b = LectorEventosInstagram.Interpretar("Feria\n12/11 18:00\nEntrada con inscripción", Perfil, Ahora);

        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.Equal(a.FuenteUrl, b.FuenteUrl);
    }

    [Fact]
    public void El_identificador_cambia_si_es_otro_evento()
    {
        var a = LectorEventosInstagram.Interpretar("Feria\n12/11 18:00", Perfil, Ahora);
        var b = LectorEventosInstagram.Interpretar("Feria\n13/11 18:00", Perfil, Ahora);

        Assert.NotEqual(a!.FuenteUrl, b!.FuenteUrl);
    }

    [Fact]
    public void Un_texto_malo_no_impide_leer_los_demas_ni_se_repiten_los_iguales()
    {
        var html = """
            {"edge_media_to_caption":{"edges":[{"node":{"text":"Feria\n31/02 18:00"}}]}}
            {"edge_media_to_caption":{"edges":[{"node":{"text":"Charla\n12/11 18:00 a 19:00"}}]}}
            {"edge_media_to_caption":{"edges":[{"node":{"text":"Charla\n12/11 18:00 a 19:00"}}]}}
            """;

        var eventos = LectorEventosInstagram.Extraer(html, Perfil, Ahora);

        var evento = Assert.Single(eventos);
        Assert.Equal("Charla", evento.Titulo);
    }
}
