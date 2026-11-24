using Breaku.Domain;

namespace Breaku.Tests;

public sealed class ReglasEventoDuplicadosTests
{
    private static readonly TimeSpan Bolivia = TimeSpan.FromHours(-4);
    private static readonly DateTimeOffset Inicio = new(2026, 11, 18, 13, 0, 0, Bolivia);
    private static readonly DateTimeOffset Fin = Inicio.AddHours(1);

    private static ReglasEvento.CandidatoDuplicado Candidato(
        string titulo = "Feria de ciencias",
        int id = 1,
        int creadorId = 99,
        DateTime? inicio = null,
        DateTime? fin = null,
        int? lugarId = 3,
        string? lugarTexto = null,
        string? nombreLugar = "Biblioteca",
        string estado = Evento.EstadoPublicado)
        => new(id, creadorId, titulo, inicio ?? Inicio.UtcDateTime, fin ?? Fin.UtcDateTime,
            lugarId, lugarTexto, nombreLugar, estado);

    private static bool EsDuplicado(
        ReglasEvento.CandidatoDuplicado candidato,
        string titulo = "Feria de ciencias",
        int? lugarId = 3,
        string lugarTexto = "Biblioteca",
        DateTimeOffset? inicio = null,
        DateTimeOffset? fin = null,
        int? idActual = null)
        => ReglasEvento.EsDuplicado(candidato, titulo, lugarId, lugarTexto,
            inicio ?? Inicio, fin ?? Fin, idActual);

    [Fact]
    public void El_titulo_se_normaliza_sin_tildes_signos_ni_palabras_vacias()
    {
        Assert.Equal("feria ciencias programacion", ReglasEvento.NormalizarTitulo("¡La Feria de Ciencias y Programación!"));
        Assert.Equal("feria ciencias programacion", ReglasEvento.NormalizarTitulo("feria ciencias programacion"));
    }

    [Theory]
    [InlineData("Feria de ciencias", 1)]
    [InlineData("La feria de las ciencias", 1)]
    [InlineData("Feria de ciencias tecnologia", 0.66667)]
    [InlineData("Feria de arte", 0.33333)]
    public void La_similitud_de_Jaccard_mide_las_palabras_del_titulo(string titulo, double esperado)
    {
        Assert.Equal(esperado, ReglasEvento.SimilitudTitulos("Feria de ciencias", titulo), 5);
    }

    [Fact]
    public void Un_titulo_con_similitud_de_0_7_o_mas_cuenta_como_duplicado()
    {
        var candidato = Candidato("Feria de ciencias tecnologia robotica");

        Assert.True(EsDuplicado(candidato, titulo: "Feria de ciencias tecnologia"));
        Assert.False(EsDuplicado(candidato, titulo: "Feria de arte"));
    }

    [Fact]
    public void La_misma_persona_detecta_el_reenvio_con_menos_de_tres_horas_de_diferencia()
    {
        var candidato = Candidato(creadorId: 7);
        var masTemprano = Inicio.AddHours(-2);

        Assert.True(EsDuplicado(candidato, inicio: masTemprano, fin: masTemprano.AddHours(1)));
        Assert.False(EsDuplicado(candidato, inicio: Inicio.AddHours(4), fin: Inicio.AddHours(5)));
    }

    [Fact]
    public void Dos_fechas_que_se_cruzan_son_duplicados_aunque_empiecen_con_mas_de_tres_horas_de_diferencia()
    {
        var candidato = Candidato(fin: Inicio.AddHours(5).UtcDateTime);
        var inicio = Inicio.AddHours(4);

        Assert.True(EsDuplicado(candidato, inicio: inicio, fin: inicio.AddHours(1)));
    }

    // ── Elegir cuál de varios candidatos decide (propios primero, luego el más cercano) ──

    [Fact]
    public void Sin_candidatos_no_hay_duplicado()
        => Assert.Null(ReglasEvento.ElegirDuplicado([], 7, Inicio));

    [Fact]
    public void Si_solo_hay_uno_ajeno_se_elige_ese()
    {
        var ajeno = Candidato(id: 1, creadorId: 99);

        Assert.Same(ajeno, ReglasEvento.ElegirDuplicado([ajeno], 7, Inicio));
    }

    [Fact]
    public void Si_solo_hay_uno_propio_se_elige_ese()
    {
        var propio = Candidato(id: 1, creadorId: 7);

        Assert.Same(propio, ReglasEvento.ElegirDuplicado([propio], 7, Inicio));
    }

    [Fact]
    public void El_propio_manda_sobre_el_ajeno_aunque_el_ajeno_este_primero_y_mas_cerca()
    {
        var ajeno = Candidato(id: 1, creadorId: 99, inicio: Inicio.UtcDateTime);
        var propio = Candidato(id: 2, creadorId: 7, inicio: Inicio.AddHours(2).UtcDateTime, fin: Inicio.AddHours(3).UtcDateTime);

        Assert.Same(propio, ReglasEvento.ElegirDuplicado([ajeno, propio], 7, Inicio));
    }

    [Fact]
    public void Entre_varios_ajenos_se_elige_el_de_inicio_mas_cercano_y_si_empatan_el_de_menor_id()
    {
        var lejos = Candidato(id: 1, creadorId: 90, inicio: Inicio.AddHours(-2).UtcDateTime, fin: Inicio.AddHours(-1).UtcDateTime);
        var cerca = Candidato(id: 2, creadorId: 91, inicio: Inicio.AddMinutes(30).UtcDateTime, fin: Inicio.AddHours(1.5).UtcDateTime);
        var empateMayorId = Candidato(id: 4, creadorId: 92, inicio: Inicio.AddMinutes(-30).UtcDateTime, fin: Inicio.AddHours(0.5).UtcDateTime);

        Assert.Same(cerca, ReglasEvento.ElegirDuplicado([lejos, empateMayorId, cerca], 7, Inicio));
    }

    [Fact]
    public void Un_mismo_lugar_cuenta_por_id_y_por_texto_normalizado()
    {
        Assert.True(EsDuplicado(Candidato(), lugarId: 3, lugarTexto: "Otro nombre"));
        Assert.True(EsDuplicado(Candidato(lugarId: null, lugarTexto: "Auditorio Céntrico", nombreLugar: null),
            lugarId: null, lugarTexto: " auditorio  centrico "));
        Assert.False(EsDuplicado(Candidato(), lugarId: 4, lugarTexto: "Biblioteca"));
    }

    [Theory]
    [InlineData(Evento.EstadoRechazado)]
    [InlineData(Evento.EstadoCancelado)]
    public void Los_eventos_rechazados_y_cancelados_no_cuentan(string estado)
    {
        Assert.False(EsDuplicado(Candidato(estado: estado)));
    }

    [Fact]
    public void Un_evento_no_se_compara_consigo_mismo_al_editarse()
    {
        Assert.False(EsDuplicado(Candidato(id: 4), idActual: 4));
        Assert.True(EsDuplicado(Candidato(id: 4), idActual: 5));
    }

    [Fact]
    public void Las_sesiones_de_un_mismo_evento_en_dias_distintos_no_son_duplicados()
    {
        var otroDia = Inicio.AddDays(2);

        Assert.False(EsDuplicado(Candidato(), inicio: otroDia, fin: otroDia.AddHours(1)));
    }

    [Fact]
    public void Los_motivos_identifican_al_evento_existente_y_caben_en_el_campo_de_motivo()
    {
        const string titulo = "Feria de ciencias";

        Assert.Contains("Posible duplicado de «Feria de ciencias» (18/11/2026).",
            ReglasEvento.MotivoPosibleDuplicado(titulo, Inicio));
        Assert.Contains("#7", ReglasEvento.MensajeEventoDuplicado(titulo, 7));
        Assert.Contains("Se publicó igual", ReglasEvento.AvisoDuplicadoAdmin(titulo, 7));

        var motivo = ReglasEvento.UnirMotivos(new string('a', 480), ReglasEvento.MotivoPosibleDuplicado(titulo, Inicio));
        Assert.Equal(ReglasEvento.MaximoMotivo, motivo.Length);
        Assert.EndsWith("…", motivo);
    }
}
