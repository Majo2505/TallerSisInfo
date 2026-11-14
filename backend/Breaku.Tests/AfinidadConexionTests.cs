using Breaku.Application.Horarios;
using Breaku.Application.Matching;
using Breaku.Application.Puentes;
using Breaku.Application.TiempoReal;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Tests;

/// <summary>Respuestas armadas para las pruebas: todas las obligatorias, cambiando solo lo que interesa.</summary>
internal static class Respuestas
{
    public static Dictionary<string, List<string>> Base() => new()
    {
        ["planes"] = ["videojuegos", "musica", "comer"],
        ["temas"] = ["tecnologia", "cine", "memes"],
        ["musica"] = ["rock", "indie"],
        ["grupo"] = ["rompo"],
        ["ideal"] = ["muchos"],
        ["confianza"] = ["humor", "hacer"],
        ["busco"] = ["amistad", "estudio"],
        ["energia"] = ["noche"],
        ["estudio"] = ["musica"],
    };

    /// <summary>Nada en común con <see cref="Base"/> en ninguna pregunta.</summary>
    public static Dictionary<string, List<string>> Opuesta() => new()
    {
        ["planes"] = ["deporte", "leer", "caminar"],
        ["temas"] = ["actualidad", "ciencia", "moda"],
        ["musica"] = ["urbano", "cumbia"],
        ["grupo"] = ["espero"],
        ["ideal"] = ["solo"],
        ["confianza"] = ["profundo", "apoyo"],
        ["busco"] = ["deporte", "proyectos"],
        ["energia"] = ["manana"],
        ["estudio"] = ["silencio"],
    };

    public static Dictionary<string, List<string>?> ParaValidar(Dictionary<string, List<string>> r) =>
        r.ToDictionary(kv => kv.Key, kv => (List<string>?)kv.Value);
}

public class ValidarTestTests
{
    [Fact]
    public void Un_test_completo_es_valido_sin_las_opcionales()
    {
        var limpias = ReglasAfinidad.Validar(Respuestas.ParaValidar(Respuestas.Base()));
        Assert.Equal(9, limpias.Count);
        Assert.False(limpias.ContainsKey("valores"));
    }

    [Fact]
    public void Las_opcionales_se_guardan_si_se_responden()
    {
        var r = Respuestas.Base();
        r["valores"] = ["honestidad"];
        r["creencias"] = ["ateo"];
        var limpias = ReglasAfinidad.Validar(Respuestas.ParaValidar(r));
        Assert.Equal(["ateo"], limpias["creencias"]);
    }

    [Fact]
    public void Una_opcional_vacia_se_toma_como_saltada()
    {
        var r = Respuestas.ParaValidar(Respuestas.Base());
        r["valores"] = [];
        r["creencias"] = null;
        Assert.False(ReglasAfinidad.Validar(r).ContainsKey("creencias"));
    }

    [Fact]
    public void Falta_una_obligatoria()
    {
        var r = Respuestas.Base();
        r.Remove("grupo");
        var ex = Assert.Throws<ArgumentException>(() => ReglasAfinidad.Validar(Respuestas.ParaValidar(r)));
        Assert.Contains("En un grupo nuevo", ex.Message);
    }

    [Fact]
    public void Una_pregunta_que_no_existe()
    {
        var r = Respuestas.ParaValidar(Respuestas.Base());
        r["signo"] = ["leo"];
        Assert.Throws<ArgumentException>(() => ReglasAfinidad.Validar(r));
    }

    [Fact]
    public void Una_opcion_que_no_existe()
    {
        var r = Respuestas.Base();
        r["planes"] = ["dormir-en-clase"];
        Assert.Throws<ArgumentException>(() => ReglasAfinidad.Validar(Respuestas.ParaValidar(r)));
    }

    [Fact]
    public void Dos_respuestas_en_una_de_una_sola()
    {
        var r = Respuestas.Base();
        r["ideal"] = ["muchos", "pocos"];
        var ex = Assert.Throws<ArgumentException>(() => ReglasAfinidad.Validar(Respuestas.ParaValidar(r)));
        Assert.Contains("una sola", ex.Message);
    }

    [Fact]
    public void Mas_del_maximo_en_una_de_varias()
    {
        var r = Respuestas.Base();
        r["confianza"] = ["humor", "hacer", "comida"];
        var ex = Assert.Throws<ArgumentException>(() => ReglasAfinidad.Validar(Respuestas.ParaValidar(r)));
        Assert.Contains("hasta 2", ex.Message);
    }

    [Fact]
    public void Opciones_repetidas()
    {
        var r = Respuestas.Base();
        r["planes"] = ["videojuegos", "videojuegos"];
        Assert.Throws<ArgumentException>(() => ReglasAfinidad.Validar(Respuestas.ParaValidar(r)));
    }

    [Fact]
    public void Sin_respuestas()
        => Assert.Throws<ArgumentException>(() => ReglasAfinidad.Validar(null));

    [Fact]
    public void Las_preguntas_tienen_ids_y_opciones_unicas()
    {
        Assert.Equal(ReglasAfinidad.Preguntas.Count, ReglasAfinidad.Preguntas.Select(p => p.Id).Distinct().Count());
        foreach (var p in ReglasAfinidad.Preguntas)
            Assert.Equal(p.Opciones.Count, p.Opciones.Select(o => o.Id).Distinct().Count());
    }
}

public class CompararTestTests
{
    [Fact]
    public void Dos_tests_iguales_son_cien()
    {
        var r = ReglasAfinidad.Comparar(Respuestas.Base(), Respuestas.Base());
        Assert.Equal(100, r.Afinidad);
        Assert.True(ReglasAfinidad.Coinciden(r));
    }

    [Fact]
    public void Sin_nada_en_comun_es_cero_y_no_se_muestra()
    {
        var r = ReglasAfinidad.Comparar(Respuestas.Base(), Respuestas.Opuesta());
        Assert.Equal(0, r.Afinidad);
        Assert.Empty(r.EnComun);
        Assert.False(ReglasAfinidad.Coinciden(r));
    }

    [Fact]
    public void Lo_que_los_conecta_va_de_lo_que_mas_pesa_y_son_maximo_tres()
    {
        var r = ReglasAfinidad.Comparar(Respuestas.Base(), Respuestas.Base());
        // planes (1,2) y confianza (1,2) pesan lo mismo y planes va primero.
        Assert.Equal(["Videojuegos", "Música", "Salir a comer"], r.EnComun);
    }

    [Fact]
    public void Una_sola_cosa_en_comun_entre_tres_y_tres_vale_sesenta_por_ciento()
    {
        var a = new Dictionary<string, List<string>> { ["planes"] = ["videojuegos", "musica", "comer"] };
        var b = new Dictionary<string, List<string>> { ["planes"] = ["videojuegos", "leer", "deporte"] };
        Assert.Equal(60, ReglasAfinidad.Comparar(a, b).Afinidad);
    }

    [Fact]
    public void En_las_de_escala_la_respuesta_vecina_vale_la_mitad()
    {
        var a = new Dictionary<string, List<string>> { ["energia"] = ["manana"] };
        Assert.Equal(50, ReglasAfinidad.Comparar(a, new Dictionary<string, List<string>> { ["energia"] = ["tarde"] }).Afinidad);
        Assert.Equal(0, ReglasAfinidad.Comparar(a, new Dictionary<string, List<string>> { ["energia"] = ["noche"] }).Afinidad);
    }

    [Fact]
    public void Una_opcional_solo_cuenta_si_las_dos_personas_la_respondieron()
    {
        var conValores = Respuestas.Base();
        conValores["valores"] = ["honestidad"];
        var sinValores = Respuestas.Base();
        Assert.Equal(100, ReglasAfinidad.Comparar(conValores, sinValores).Afinidad);

        var otrosValores = Respuestas.Base();
        otrosValores["valores"] = ["lealtad"];
        Assert.True(ReglasAfinidad.Comparar(conValores, otrosValores).Afinidad < 100);
    }

    [Fact]
    public void Otra_religion_en_las_dos_no_se_muestra_como_algo_en_comun()
    {
        var a = new Dictionary<string, List<string>> { ["creencias"] = ["otra"] };
        var r = ReglasAfinidad.Comparar(a, a);
        Assert.Equal(100, r.Afinidad);
        Assert.Empty(r.EnComun);
    }

    [Fact]
    public void Las_creencias_iguales_si_aparecen_cuando_las_dos_las_compartieron()
    {
        var a = new Dictionary<string, List<string>> { ["creencias"] = ["ateo"] };
        Assert.Equal(["Ateísmo"], ReglasAfinidad.Comparar(a, a).EnComun);
    }

    [Theory]
    [InlineData(100, 100, 100)]
    [InlineData(80, 50, 68)]
    [InlineData(35, 100, 61)]
    [InlineData(0, 0, 0)]
    public void La_compatibilidad_es_sesenta_test_y_cuarenta_tiempo(int afinidad, int tiempo, int esperado)
        => Assert.Equal(esperado, ReglasAfinidad.Compatibilidad(afinidad, tiempo));
}

public class ReglasConexionTests
{
    [Fact]
    public void El_aviso_nunca_pasa_de_200_caracteres()
    {
        var largo = new string('a', 300);
        var m = ReglasConexion.MensajeSolicitada(largo, 87, 3, new TimeOnly(10, 30), new TimeOnly(11, 45));
        Assert.True(m.Length <= 200);
    }

    [Fact]
    public void El_aviso_dice_quien_cuanto_y_cuando()
        => Assert.Equal("Ana Q. (87 % compatible) quiere coincidir contigo el miércoles de 10:30 a 11:45. Respóndele en Libres contigo.",
            ReglasConexion.MensajeSolicitada("Ana Q.", 87, 3, new TimeOnly(10, 30), new TimeOnly(11, 45)));

    [Fact]
    public void Solo_quien_recibio_responde_y_una_vez()
    {
        Assert.Null(ReglasConexion.ErrorAlResponder(ReglasConexion.Pendiente, soyDestinatario: true));
        Assert.Equal("NO_ENCONTRADO", ReglasConexion.ErrorAlResponder(ReglasConexion.Pendiente, soyDestinatario: false));
        Assert.Equal(ReglasConexion.ErrorYaRespondida, ReglasConexion.ErrorAlResponder(ReglasConexion.Aceptada, soyDestinatario: true));
    }

    [Fact]
    public void Si_ya_te_saludaron_te_dice_que_respondas()
        => Assert.Contains("ya te saludó", ReglasConexion.MensajeYaExiste(ReglasConexion.Pendiente, laEnvieYo: false));
}

/// <summary>MatchingService y ConexionService de punta a punta, con un campus en memoria.</summary>
public class LibresContigoServiciosTests
{
    private const int Yo = 1, Ana = 2, Beto = 3, Ciro = 4, Dani = 5;
    private static readonly Zona Campus = new() { Id = 3, Nombre = "Cochabamba", Activa = true };

    private sealed class Campo
    {
        public Dictionary<int, Usuario> Usuarios { get; } = [];
        public Dictionary<int, List<Puente>> Puentes { get; } = [];
        public Dictionary<int, TestGuardado> Tests { get; } = [];
        public List<ConexionGuardada> Conexiones { get; } = [];
        public List<(int Usuario, string Mensaje)> Avisos { get; } = [];
        public List<int> TiempoReal { get; } = [];

        public void Persona(int id, string nombre, Dictionary<string, List<string>>? test, params (int Dia, string Ini, string Fin)[] puentes)
        {
            Usuarios[id] = new Usuario
            {
                Id = id, Nombre = nombre, Carrera = "Ing. de Sistemas", Rol = "ESTUDIANTE", Activo = true,
                VisibleMatching = true, ZonaActualId = Campus.Id, ZonaActual = Campus, Email = $"u{id}@ucb.edu.bo",
            };
            Puentes[id] = puentes.Select((p, i) => new Puente
            {
                Id = id * 100 + i, UsuarioId = id, DiaSemana = (short)p.Dia,
                HoraInicio = TimeOnly.Parse(p.Ini), HoraFin = TimeOnly.Parse(p.Fin),
            }).ToList();
            if (test is not null) Tests[id] = new TestGuardado(ReglasAfinidad.Version, test, DateTime.UtcNow);
        }
    }

    private sealed class Repos(Campo c) : IMatchingRepository, IPuenteRepository, IAfinidadRepository, IConexionRepository, ITiempoReal
    {
        public Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default) => Task.FromResult(c.Usuarios.GetValueOrDefault(usuarioId));
        public Task<List<CompaneroCandidato>> ListarCandidatosAsync(int usuarioId, int zonaId, Puente puente, CancellationToken ct = default) =>
            Task.FromResult(c.Usuarios.Values
                .Where(u => u.Id != usuarioId && u.VisibleMatching && u.ZonaActualId == zonaId)
                .Select(u => new CompaneroCandidato(u.Id, u.Nombre, u.Carrera, u.Semestre, c.Puentes[u.Id])).ToList());
        public Task GuardarConHistorialAsync(int usuarioId, string valoresAnteriores, string valoresNuevos, CancellationToken ct = default) => Task.CompletedTask;

        public Task<List<Puente>> ListarPorUsuarioAsync(int usuarioId, CancellationToken ct = default) => Task.FromResult(c.Puentes[usuarioId]);
        public Task ReemplazarAsync(int usuarioId, IReadOnlyList<Puente> nuevos, CancellationToken ct = default) => Task.CompletedTask;

        public Task<TestGuardado?> ObtenerTestAsync(int usuarioId, CancellationToken ct = default) => Task.FromResult(c.Tests.GetValueOrDefault(usuarioId));
        public Task GuardarTestAsync(int usuarioId, int version, Dictionary<string, List<string>> respuestas, DateTime ahoraUtc, CancellationToken ct = default)
        { c.Tests[usuarioId] = new TestGuardado(version, respuestas, ahoraUtc); return Task.CompletedTask; }
        public Task<Dictionary<int, Dictionary<string, List<string>>>> ObtenerTestsAsync(IReadOnlyCollection<int> ids, int version, CancellationToken ct = default) =>
            Task.FromResult(c.Tests.Where(t => ids.Contains(t.Key) && t.Value.Version == version).ToDictionary(t => t.Key, t => t.Value.Respuestas));

        public Task<List<ConexionGuardada>> ListarDeUsuarioAsync(int usuarioId, CancellationToken ct = default) =>
            Task.FromResult(c.Conexiones.Where(x => x.SolicitanteId == usuarioId || x.DestinatarioId == usuarioId).ToList());
        public Task<ConexionGuardada?> ObtenerAsync(int id, CancellationToken ct = default) => Task.FromResult(c.Conexiones.FirstOrDefault(x => x.Id == id));
        public Task<ConexionGuardada?> EntreAsync(int a, int b, CancellationToken ct = default) =>
            Task.FromResult(c.Conexiones.FirstOrDefault(x => (x.SolicitanteId == a && x.DestinatarioId == b) || (x.SolicitanteId == b && x.DestinatarioId == a)));
        public Task<int> ContarPendientesEnviadasAsync(int usuarioId, CancellationToken ct = default) =>
            Task.FromResult(c.Conexiones.Count(x => x.SolicitanteId == usuarioId && x.Estado == ReglasConexion.Pendiente));
        public Task<Dictionary<int, PersonaConexion>> ObtenerPersonasAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default) =>
            Task.FromResult(ids.Distinct().ToDictionary(i => i, i => new PersonaConexion(i, c.Usuarios[i].Nombre, c.Usuarios[i].Carrera, null, c.Usuarios[i].Email)));
        public Task<int> CrearAsync(ConexionNueva n, string mensajeAviso, DateTime ahoraUtc, CancellationToken ct = default)
        {
            var id = c.Conexiones.Count + 1;
            c.Conexiones.Add(new ConexionGuardada(id, n.SolicitanteId, n.DestinatarioId, n.DiaSemana, n.HoraInicio, n.HoraFin,
                n.Compatibilidad, n.EnComun, ReglasConexion.Pendiente, ahoraUtc, null));
            c.Avisos.Add((n.DestinatarioId, mensajeAviso));
            return Task.FromResult(id);
        }
        public Task ResponderAsync(int conexionId, bool aceptar, string? mensajeAvisoAceptada, DateTime ahoraUtc, CancellationToken ct = default)
        {
            var i = c.Conexiones.FindIndex(x => x.Id == conexionId);
            c.Conexiones[i] = c.Conexiones[i] with { Estado = aceptar ? ReglasConexion.Aceptada : ReglasConexion.Rechazada, RespondidaEn = ahoraUtc };
            if (mensajeAvisoAceptada is not null) c.Avisos.Add((c.Conexiones[i].SolicitanteId, mensajeAvisoAceptada));
            return Task.CompletedTask;
        }

        public Task AvisosCambiaronAsync(IReadOnlyCollection<int> usuarioIds, CancellationToken ct = default) { c.TiempoReal.AddRange(usuarioIds); return Task.CompletedTask; }
        public Task ComunidadCreadaAsync(int comunidadId, CancellationToken ct = default) => Task.CompletedTask;
        public Task AvisosDeComunidadAsync(int comunidadId, CancellationToken ct = default) => Task.CompletedTask;
        public Task ComunidadActualizadaAsync(int comunidadId, string estado, int confirmados, CancellationToken ct = default) => Task.CompletedTask;
    }

    /// <summary>
    /// Yo: martes 10:00–12:00. Ana: mismo test y mismo horario. Beto: test opuesto (no coincide en el test).
    /// Ciro: mismo test pero libre otro día (no coincide en tiempo). Dani: libre conmigo, sin test.
    /// </summary>
    private static (MatchingService Matching, ConexionService Conexiones, Campo Campo) Armar()
    {
        var c = new Campo();
        c.Persona(Yo, "Maite Arauco", Respuestas.Base(), (2, "10:00", "12:00"));
        c.Persona(Ana, "Ana Quiroga Paz", Respuestas.Base(), (2, "10:00", "12:00"));
        c.Persona(Beto, "Beto Rojas", Respuestas.Opuesta(), (2, "10:00", "12:00"));
        c.Persona(Ciro, "Ciro Vargas", Respuestas.Base(), (4, "10:00", "12:00"));
        c.Persona(Dani, "Dani Flores", null, (2, "10:00", "12:00"));
        var r = new Repos(c);
        var matching = new MatchingService(r, r, r, r);
        return (matching, new ConexionService(r, r, matching, r), c);
    }

    private const int MiPuente = Yo * 100;

    [Fact]
    public async Task Sin_test_no_muestra_a_nadie()
    {
        var (matching, _, c) = Armar();
        c.Tests.Remove(Yo);
        var r = await matching.BuscarAsync(Yo, MiPuente);
        Assert.Equal(MatchingService.EstadoSinTest, r.Estado);
        Assert.Empty(r.Companeros);
    }

    [Fact]
    public async Task Solo_aparece_quien_coincide_en_tiempo_y_en_test()
    {
        var (matching, _, _) = Armar();
        var r = await matching.BuscarAsync(Yo, MiPuente);
        Assert.Equal(MatchingService.EstadoOk, r.Estado);
        var unico = Assert.Single(r.Companeros);
        Assert.Equal(Ana, unico.UsuarioId);
        Assert.Equal(100, unico.Afinidad);
        Assert.Equal(100, unico.Compatibilidad);
        Assert.Equal("Ana Q.", unico.Nombre);
        Assert.NotEmpty(unico.EnComun);
        Assert.Null(unico.Conexion);
    }

    [Fact]
    public async Task Si_hay_gente_libre_pero_nadie_coincide_en_el_test_lo_dice()
    {
        var (matching, _, c) = Armar();
        c.Tests[Ana] = new TestGuardado(ReglasAfinidad.Version, Respuestas.Opuesta(), DateTime.UtcNow);
        var r = await matching.BuscarAsync(Yo, MiPuente);
        Assert.Equal(MatchingService.EstadoSinResultados, r.Estado);
        Assert.Equal(MatchingService.MensajeSinCoincidencias, r.Mensaje);
    }

    [Fact]
    public async Task Un_test_de_otra_version_no_cuenta()
    {
        var (matching, _, c) = Armar();
        c.Tests[Ana] = new TestGuardado(ReglasAfinidad.Version + 1, Respuestas.Base(), DateTime.UtcNow);
        var r = await matching.BuscarAsync(Yo, MiPuente);
        Assert.Empty(r.Companeros);
    }

    [Fact]
    public async Task Saludar_crea_el_saludo_y_avisa_sin_revelar_el_nombre_completo()
    {
        var (_, conexiones, c) = Armar();
        var dto = await conexiones.SaludarAsync(Yo, new SaludarRequest(Ana, MiPuente));

        Assert.Equal(ReglasConexion.Pendiente, dto.Estado);
        Assert.True(dto.EnviadaPorMi);
        Assert.Equal("Ana Q.", dto.Nombre);
        Assert.Null(dto.Correo);
        var (para, mensaje) = Assert.Single(c.Avisos);
        Assert.Equal(Ana, para);
        Assert.StartsWith("Maite A. (100 % compatible) quiere coincidir contigo el martes de 10:00 a 12:00", mensaje);
        Assert.Contains(Ana, c.TiempoReal);
    }

    [Fact]
    public async Task No_se_puede_saludar_a_quien_no_te_sale_en_el_matching()
    {
        var (_, conexiones, _) = Armar();
        foreach (var otro in new[] { Beto, Ciro, Dani })
        {
            var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => conexiones.SaludarAsync(Yo, new SaludarRequest(otro, MiPuente)));
            Assert.Equal(ReglasConexion.ErrorNoCoinciden, ex.Codigo);
        }
    }

    [Fact]
    public async Task No_se_saluda_dos_veces_ni_al_reves()
    {
        var (_, conexiones, _) = Armar();
        await conexiones.SaludarAsync(Yo, new SaludarRequest(Ana, MiPuente));

        var otraVez = await Assert.ThrowsAsync<ReglaNegocioException>(() => conexiones.SaludarAsync(Yo, new SaludarRequest(Ana, MiPuente)));
        Assert.Equal(ReglasConexion.ErrorYaExiste, otraVez.Codigo);

        var alReves = await Assert.ThrowsAsync<ReglaNegocioException>(() => conexiones.SaludarAsync(Ana, new SaludarRequest(Yo, Ana * 100)));
        Assert.Contains("ya te saludó", alReves.Message);
    }

    [Fact]
    public async Task Con_cinco_saludos_sin_respuesta_no_deja_saludar_mas()
    {
        var (_, conexiones, c) = Armar();
        for (var i = 0; i < ReglasConexion.MaximoPendientesEnviados; i++)
            c.Conexiones.Add(new ConexionGuardada(100 + i, Yo, 900 + i, 2, new(10, 0), new(11, 0), 50, [], ReglasConexion.Pendiente, DateTime.UtcNow, null));
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => conexiones.SaludarAsync(Yo, new SaludarRequest(Ana, MiPuente)));
        Assert.Equal(ReglasConexion.ErrorDemasiados, ex.Codigo);
    }

    [Fact]
    public async Task No_puedes_saludarte_a_ti_mismo()
    {
        var (_, conexiones, _) = Armar();
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => conexiones.SaludarAsync(Yo, new SaludarRequest(Yo, MiPuente)));
        Assert.Equal("VALIDACION", ex.Codigo);
    }

    [Fact]
    public async Task Al_aceptar_los_dos_ven_nombre_completo_y_correo()
    {
        var (_, conexiones, c) = Armar();
        var saludo = await conexiones.SaludarAsync(Yo, new SaludarRequest(Ana, MiPuente));

        var respuesta = await conexiones.ResponderAsync(Ana, saludo.Id, new ResponderConexionRequest(true));
        Assert.Equal("Maite Arauco", respuesta.Nombre);
        Assert.Equal("u1@ucb.edu.bo", respuesta.Correo);

        var mias = await conexiones.ListarAsync(Yo);
        var conectada = Assert.Single(mias.Aceptadas);
        Assert.Equal("Ana Quiroga Paz", conectada.Nombre);
        Assert.Equal("u2@ucb.edu.bo", conectada.Correo);
        Assert.Contains(c.Avisos, a => a.Usuario == Yo && a.Mensaje.StartsWith("Ana Q. aceptó tu saludo"));
    }

    [Fact]
    public async Task El_rechazo_es_silencioso_para_quien_saludo()
    {
        var (matching, conexiones, c) = Armar();
        var saludo = await conexiones.SaludarAsync(Yo, new SaludarRequest(Ana, MiPuente));
        await conexiones.ResponderAsync(Ana, saludo.Id, new ResponderConexionRequest(false));

        var mias = await conexiones.ListarAsync(Yo);
        Assert.Equal(ReglasConexion.Pendiente, Assert.Single(mias.Enviadas).Estado);
        Assert.Null(mias.Enviadas[0].Correo);
        Assert.Equal(ReglasConexion.Pendiente, (await matching.BuscarAsync(Yo, MiPuente)).Companeros[0].Conexion!.Estado);
        Assert.DoesNotContain(c.Avisos, a => a.Usuario == Yo);
    }

    [Fact]
    public async Task Solo_quien_recibio_puede_responder_y_una_sola_vez()
    {
        var (_, conexiones, _) = Armar();
        var saludo = await conexiones.SaludarAsync(Yo, new SaludarRequest(Ana, MiPuente));

        var propia = await Assert.ThrowsAsync<ReglaNegocioException>(() => conexiones.ResponderAsync(Yo, saludo.Id, new ResponderConexionRequest(true)));
        Assert.Equal("VALIDACION", propia.Codigo);

        var ajena = await Assert.ThrowsAsync<ReglaNegocioException>(() => conexiones.ResponderAsync(Beto, saludo.Id, new ResponderConexionRequest(true)));
        Assert.Equal("NO_ENCONTRADO", ajena.Codigo);

        await conexiones.ResponderAsync(Ana, saludo.Id, new ResponderConexionRequest(true));
        var dos = await Assert.ThrowsAsync<ReglaNegocioException>(() => conexiones.ResponderAsync(Ana, saludo.Id, new ResponderConexionRequest(false)));
        Assert.Equal(ReglasConexion.ErrorYaRespondida, dos.Codigo);
    }

    [Fact]
    public async Task Quien_recibe_ve_el_saludo_pendiente_en_recibidas()
    {
        var (_, conexiones, _) = Armar();
        await conexiones.SaludarAsync(Yo, new SaludarRequest(Ana, MiPuente));
        var deAna = await conexiones.ListarAsync(Ana);
        var recibida = Assert.Single(deAna.Recibidas);
        Assert.Equal("Maite A.", recibida.Nombre);
        Assert.False(recibida.EnviadaPorMi);
        Assert.Empty(deAna.Aceptadas);
    }

    [Fact]
    public async Task Guardar_el_test_valida_y_lo_devuelve_completado()
    {
        var c = new Campo();
        var servicio = new AfinidadService(new Repos(c));
        var malo = new GuardarTestRequest(new Dictionary<string, List<string>?> { ["planes"] = ["videojuegos"] });
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.GuardarAsync(Yo, malo));
        Assert.Equal("VALIDACION", ex.Codigo);

        var dto = await servicio.GuardarAsync(Yo, new GuardarTestRequest(Respuestas.ParaValidar(Respuestas.Base())));
        Assert.True(dto.Completado);
        Assert.Equal(ReglasAfinidad.Preguntas.Count, dto.Preguntas.Count);
        Assert.Equal(["videojuegos", "musica", "comer"], dto.Respuestas!["planes"]);
    }
}
