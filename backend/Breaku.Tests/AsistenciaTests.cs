using Breaku.Application.Comunidades;
using Breaku.Application.Horarios;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Tests;

public class ReglasAsistenciaTests
{
    private const string Pendiente = Participacion.EstadoPendiente;
    private const string Confirmado = Participacion.EstadoConfirmado;
    private const string Rechazado = Participacion.EstadoRechazado;

    private static ReglasAsistencia.Situacion Invitado(string? miEstado = Pendiente, int confirmados = 1, int maximo = 4,
        bool cruce = false, bool puedeVerla = false)
        => new(Existe: true, EsCreador: false, Terminada: false, MiEstado: miEstado, PuedeVerla: puedeVerla,
            Confirmados: confirmados, Maximo: maximo, HayCruce: cruce);

    [Fact]
    public void El_invitado_confirma_y_queda_confirmado()
        => Assert.Equal(ReglasAsistencia.Decision.Pasa(Confirmado), ReglasAsistencia.Decidir(Invitado(), asisto: true));

    [Fact]
    public void El_invitado_rechaza_y_queda_rechazado()
        => Assert.Equal(ReglasAsistencia.Decision.Pasa(Rechazado), ReglasAsistencia.Decidir(Invitado(), asisto: false));

    [Theory]
    [InlineData(Confirmado, true)]
    [InlineData(Rechazado, false)]
    public void Responder_lo_mismo_otra_vez_no_escribe(string estado, bool asisto)
    {
        var decision = ReglasAsistencia.Decidir(Invitado(estado), asisto);

        Assert.Null(decision.Error);
        Assert.False(decision.Cambia);
        Assert.Equal(estado, decision.Estado);
    }

    [Fact]
    public void Quien_rechazo_puede_cambiar_de_idea_si_queda_lugar()
        => Assert.Equal(ReglasAsistencia.Decision.Pasa(Confirmado), ReglasAsistencia.Decidir(Invitado(Rechazado), asisto: true));

    [Fact]
    public void Quien_iba_puede_bajarse()
        => Assert.Equal(ReglasAsistencia.Decision.Pasa(Rechazado), ReglasAsistencia.Decidir(Invitado(Confirmado), asisto: false));

    [Fact]
    public void Sin_lugares_no_se_puede_confirmar()
    {
        var decision = ReglasAsistencia.Decidir(Invitado(confirmados: 4, maximo: 4), asisto: true);

        Assert.Equal(ReglasAsistencia.ErrorCompleta, decision.Error);
        Assert.Equal(ReglasAsistencia.MensajeCompleta, decision.Mensaje);
    }

    [Fact]
    public void Quien_ya_iba_a_un_grupo_lleno_sigue_confirmado()
        => Assert.Equal(ReglasAsistencia.Decision.Queda(Confirmado),
            ReglasAsistencia.Decidir(Invitado(Confirmado, confirmados: 4, maximo: 4), asisto: true));

    [Fact]
    public void No_se_confirma_si_a_esa_hora_ya_va_a_otra_comunidad()
        => Assert.Equal(ReglasAsistencia.ErrorCruce, ReglasAsistencia.Decidir(Invitado(cruce: true), asisto: true).Error);

    [Fact]
    public void Bajarse_siempre_se_puede_aunque_este_lleno_o_se_cruce()
        => Assert.Equal(ReglasAsistencia.Decision.Pasa(Rechazado),
            ReglasAsistencia.Decidir(Invitado(Confirmado, confirmados: 4, maximo: 4, cruce: true), asisto: false));

    [Fact]
    public void Quien_la_creo_no_responde_porque_siempre_va()
    {
        var decision = ReglasAsistencia.Decidir(Invitado(Confirmado) with { EsCreador = true }, asisto: false);

        Assert.Equal(ReglasAsistencia.ErrorValidacion, decision.Error);
        Assert.Equal(ReglasAsistencia.MensajeEresCreador, decision.Mensaje);
    }

    [Fact]
    public void Quien_heredo_la_comunidad_ve_que_ahora_la_organiza()
    {
        var decision = ReglasAsistencia.Decidir(Invitado(Confirmado) with { EsCreador = true, CreadorHeredado = true }, asisto: true);

        Assert.Equal(ReglasAsistencia.MensajeEresOrganizador, decision.Mensaje);
        Assert.Equal("Ahora organizas esta comunidad.", ReglasAsistencia.MensajeEresOrganizador);
    }

    [Fact]
    public void Despues_de_que_termina_ya_no_se_responde()
    {
        var decision = ReglasAsistencia.Decidir(Invitado() with { Terminada = true }, asisto: true);

        Assert.Equal(ReglasAsistencia.ErrorValidacion, decision.Error);
        Assert.Equal(ReglasAsistencia.MensajeTerminada, decision.Mensaje);
    }

    [Fact]
    public void Una_comunidad_que_no_existe_o_esta_archivada_es_no_encontrada()
        => Assert.Equal(ReglasAsistencia.ErrorNoEncontrado,
            ReglasAsistencia.Decidir(Invitado() with { Existe = false }, asisto: true).Error);

    [Fact]
    public void Una_comunidad_ajena_que_no_puede_ver_es_no_encontrada_aunque_exista()
        => Assert.Equal(ReglasAsistencia.ErrorNoEncontrado,
            ReglasAsistencia.Decidir(Invitado(miEstado: null, puedeVerla: false), asisto: true).Error);

    [Fact]
    public void Desde_Descubrir_se_une_sin_invitacion_si_puede_verla()
        => Assert.Equal(ReglasAsistencia.Decision.Pasa(Confirmado),
            ReglasAsistencia.Decidir(Invitado(miEstado: null, puedeVerla: true), asisto: true));

    [Fact]
    public void Sin_invitacion_no_hay_nada_que_rechazar()
    {
        var decision = ReglasAsistencia.Decidir(Invitado(miEstado: null, puedeVerla: true), asisto: false);

        Assert.Equal(ReglasAsistencia.ErrorValidacion, decision.Error);
        Assert.Equal(ReglasAsistencia.MensajeNoEstabas, decision.Mensaje);
    }

    [Theory]
    [InlineData(2, Pendiente, Confirmado, 3)]
    [InlineData(2, null, Confirmado, 3)]
    [InlineData(3, Confirmado, Rechazado, 2)]
    [InlineData(2, Pendiente, Rechazado, 2)]
    [InlineData(3, Confirmado, Confirmado, 3)]
    public void Los_confirmados_cambian_solo_si_entra_o_sale_alguien(int antes, string? estadoAntes, string estadoDespues, int esperado)
        => Assert.Equal(esperado, ReglasAsistencia.ConfirmadosDespues(antes, estadoAntes, estadoDespues));

    [Fact]
    public void Se_llena_y_se_reabre_solo_al_cruzar_el_maximo()
    {
        Assert.True(ReglasAsistencia.SeLlena(3, 4, 4));
        Assert.False(ReglasAsistencia.SeLlena(2, 3, 4));
        Assert.True(ReglasAsistencia.SeReabre(4, 3, 4));
        Assert.False(ReglasAsistencia.SeReabre(3, 2, 4));
    }

    [Fact]
    public void El_aviso_de_grupo_lleno_entra_en_200_caracteres()
    {
        var maximaDeLaBase = ReglasAsistencia.MensajeCerrada(new string('a', 100));
        var demasiadoLarga = ReglasAsistencia.MensajeCerrada(new string('a', 150));

        Assert.True(maximaDeLaBase.Length <= ReglasAsistencia.MaximoLargoAviso);
        Assert.DoesNotContain("…", maximaDeLaBase);
        Assert.True(demasiadoLarga.Length <= ReglasAsistencia.MaximoLargoAviso);
        Assert.Contains("…", demasiadoLarga);
        Assert.Equal("«jugar cartas» ya se llenó. Si alguien se baja, todavía puedes confirmar desde Comunidades.",
            ReglasAsistencia.MensajeCerrada("jugar cartas"));
    }

    [Fact]
    public void El_cruce_dice_a_que_actividad_ya_va_y_a_que_hora()
        => Assert.Equal("A esa hora ya vas a «estudiar Redes» (10:00–11:00).",
            ReglasAsistencia.MensajeCruce("estudiar Redes", new TimeOnly(10, 0), new TimeOnly(11, 0)));
}

public class AsistenciaServiceTests
{
    private static readonly DateTimeOffset AhoraUtc = new(2026, 11, 3, 13, 0, 0, TimeSpan.Zero);

    private sealed class RelojFijo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => AhoraUtc;
    }

    private sealed class RepositorioFalso(ContextoAsistencia contexto, ParticipantesComunidad? participantes = null)
        : IAsistenciaRepository
    {
        public CambioAsistencia? Cambio { get; private set; }
        public bool Llamado { get; private set; }

        public Task ResponderAsync(int comunidadId, int usuarioId, DateTime ahoraUtc, Func<ContextoAsistencia, CambioAsistencia> decidir,
            CancellationToken ct = default)
        {
            Llamado = true;
            Cambio = decidir(contexto);
            return Task.CompletedTask;
        }

        public Task<ParticipantesComunidad?> ObtenerParticipantesAsync(int comunidadId, CancellationToken ct = default)
            => Task.FromResult(participantes);
    }

    private const int Yo = 5;
    private const int Creadora = 2;

    private static ComunidadEfimera Comunidad(short maximo = 3) => new()
    {
        Id = 12, CreadorId = Creadora, Actividad = "jugar cartas", MaxParticipantes = maximo,
        Inicio = new DateTime(2026, 11, 3, 14, 0, 0, DateTimeKind.Utc),
        Fin = new DateTime(2026, 11, 3, 15, 0, 0, DateTimeKind.Utc),
        Estado = ComunidadEfimera.EstadoActiva
    };

    private static ContextoAsistencia Contexto(string? miEstado = Participacion.EstadoPendiente, int confirmados = 1, short maximo = 3,
        CruceAsistencia? cruce = null, List<int>? pendientes = null)
        => new(Comunidad(maximo), miEstado, false, confirmados, cruce, pendientes ?? [7, 8]);

    private static AsistenciaService Servicio(RepositorioFalso repo) => new(repo, new RelojFijo());

    [Fact]
    public async Task Sin_respuesta_es_validacion_y_no_toca_la_base()
    {
        var repo = new RepositorioFalso(Contexto());

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => Servicio(repo).ResponderAsync(Yo, 12, null));

        Assert.Equal("VALIDACION", ex.Codigo);
        Assert.False(repo.Llamado);
    }

    [Fact]
    public async Task Confirmar_responde_con_la_hora_de_Bolivia_y_los_confirmados_nuevos()
    {
        var repo = new RepositorioFalso(Contexto());

        var r = await Servicio(repo).ResponderAsync(Yo, 12, true);

        Assert.Equal(new AsistenciaDto(12, Participacion.EstadoConfirmado, 2, 3,
            "¡Listo! Te esperamos en «jugar cartas» hoy de 10:00 a 11:00."), r);
        Assert.True(repo.Cambio!.Guardar);
        Assert.Empty(repo.Cambio.AvisarCerrada);
        Assert.False(repo.Cambio.Reabrir);
    }

    [Fact]
    public async Task Quien_llena_el_grupo_hace_que_se_avise_a_los_pendientes()
    {
        var repo = new RepositorioFalso(Contexto(confirmados: 2, maximo: 3));

        var r = await Servicio(repo).ResponderAsync(Yo, 12, true);

        Assert.Equal(3, r.Confirmados);
        Assert.Equal([7, 8], repo.Cambio!.AvisarCerrada);
        Assert.Equal(ReglasAsistencia.MensajeCerrada("jugar cartas"), repo.Cambio.MensajeCerrada);
    }

    [Fact]
    public async Task Bajarse_de_un_grupo_lleno_lo_reabre()
    {
        var repo = new RepositorioFalso(Contexto(Participacion.EstadoConfirmado, confirmados: 3, maximo: 3));

        var r = await Servicio(repo).ResponderAsync(Yo, 12, false);

        Assert.Equal(new AsistenciaDto(12, Participacion.EstadoRechazado, 2, 3, ReglasAsistencia.MensajeRechazado), r);
        Assert.True(repo.Cambio!.Reabrir);
        Assert.Empty(repo.Cambio.AvisarCerrada);
    }

    [Fact]
    public async Task Repetir_la_respuesta_no_guarda_nada()
    {
        var repo = new RepositorioFalso(Contexto(Participacion.EstadoConfirmado, confirmados: 2));

        var r = await Servicio(repo).ResponderAsync(Yo, 12, true);

        Assert.Equal(2, r.Confirmados);
        Assert.False(repo.Cambio!.Guardar);
    }

    [Fact]
    public async Task El_cruce_explica_con_la_hora_de_Bolivia_a_que_ya_va()
    {
        var cruce = new CruceAsistencia("estudiar Redes",
            new DateTime(2026, 11, 3, 14, 30, 0, DateTimeKind.Utc), new DateTime(2026, 11, 3, 15, 30, 0, DateTimeKind.Utc));
        var repo = new RepositorioFalso(Contexto(cruce: cruce));

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => Servicio(repo).ResponderAsync(Yo, 12, true));

        Assert.Equal("CRUCE", ex.Codigo);
        Assert.Equal("A esa hora ya vas a «estudiar Redes» (10:30–11:30).", ex.Message);
        Assert.Null(repo.Cambio);
    }

    [Fact]
    public async Task Una_comunidad_que_ya_termino_no_acepta_respuestas()
    {
        var terminada = Contexto();
        terminada.Comunidad!.Inicio = new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);
        terminada.Comunidad.Fin = new DateTime(2026, 11, 3, 12, 59, 0, DateTimeKind.Utc);

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => Servicio(new RepositorioFalso(terminada)).ResponderAsync(Yo, 12, true));

        Assert.Equal(ReglasAsistencia.MensajeTerminada, ex.Message);
    }

    [Fact]
    public async Task Si_la_comunidad_no_existe_es_no_encontrado()
    {
        var repo = new RepositorioFalso(new ContextoAsistencia(null, null, false, 0, null, []));

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => Servicio(repo).ResponderAsync(Yo, 99, true));

        Assert.Equal("NO_ENCONTRADO", ex.Codigo);
    }

    private static ParticipantesComunidad Grupo(bool creadoraEnTabla = true)
    {
        var filas = new List<FilaParticipante>
        {
            new(9, "Lucía Rojas", "Derecho", Participacion.EstadoRechazado, new DateTime(2026, 11, 3, 12, 40, 0, DateTimeKind.Utc)),
            new(7, "Mateo Vargas", "Arquitectura", Participacion.EstadoPendiente, null),
            new(Yo, "Diego Mamani", "Ingeniería Industrial", Participacion.EstadoConfirmado, new DateTime(2026, 11, 3, 12, 30, 0, DateTimeKind.Utc)),
            new(8, "Ana Quiroga", "Medicina", Participacion.EstadoConfirmado, new DateTime(2026, 11, 3, 12, 10, 0, DateTimeKind.Utc)),
        };
        if (creadoraEnTabla)
            filas.Add(new(Creadora, "Camila Torres", "Psicología", Participacion.EstadoConfirmado, new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc)));
        return new ParticipantesComunidad(Comunidad(5), "Camila Torres", filas);
    }

    [Fact]
    public async Task Quien_la_creo_ve_a_todos_con_nombre_y_carrera_en_orden()
    {
        var r = await Servicio(new RepositorioFalso(Contexto(), Grupo())).ListarParticipantesAsync(Creadora, 12);

        Assert.True(r.EsMia);
        Assert.Equal(3, r.Confirmados);
        Assert.Equal(("10:00", "11:00"), (r.HoraInicio, r.HoraFin));
        Assert.Equal(["Tú", "Ana Quiroga", "Diego Mamani", "Mateo Vargas", "Lucía Rojas"], r.Participantes.Select(p => p.Nombre));
        Assert.True(r.Participantes[0].EsCreador && r.Participantes[0].EsTu);
        Assert.Equal("Medicina", r.Participantes[1].Carrera);
        Assert.Equal(Participacion.EstadoRechazado, r.Participantes[^1].Estado);
    }

    [Fact]
    public async Task Un_invitado_ve_solo_a_los_que_van_con_nombre_corto_y_a_si_mismo()
    {
        var r = await Servicio(new RepositorioFalso(Contexto(), Grupo())).ListarParticipantesAsync(7, 12);

        Assert.False(r.EsMia);
        Assert.Equal(["Camila T.", "Ana Q.", "Diego M.", "Tú"], r.Participantes.Select(p => p.Nombre));
        Assert.All(r.Participantes, p => Assert.Null(p.Carrera));
        Assert.Equal(Participacion.EstadoPendiente, r.Participantes[^1].Estado);
    }

    [Fact]
    public async Task Quien_no_esta_en_la_comunidad_no_ve_la_lista()
    {
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => Servicio(new RepositorioFalso(Contexto(), Grupo())).ListarParticipantesAsync(40, 12));

        Assert.Equal("NO_ENCONTRADO", ex.Codigo);
    }

    [Fact]
    public async Task La_creadora_cuenta_aunque_su_comunidad_sea_anterior_a_su_fila()
    {
        var r = await Servicio(new RepositorioFalso(Contexto(), Grupo(creadoraEnTabla: false))).ListarParticipantesAsync(Creadora, 12);

        Assert.Equal(3, r.Confirmados);
        Assert.Equal(5, r.Participantes.Count);
    }
}
