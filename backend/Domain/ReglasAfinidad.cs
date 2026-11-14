namespace Breaku.Domain.Rules;

/// <summary>
/// HU12 (ampliación): el test de afinidad de «Libres contigo». Unas 11 preguntas de opción múltiple, unos 2 minutos.
/// Lo que más predice una amistad universitaria es coincidir en el mismo lugar y a la misma hora (eso ya lo da el puente
/// compartido); después pesan los gustos, el estilo de vida (salir o no, cómo estudia) y la forma de conectar. Por eso el
/// test pregunta por eso y no por un perfil psicológico largo. Valores y creencias son opcionales.
/// </summary>
public static class ReglasAfinidad
{
    /// <summary>Si cambian las preguntas, se sube la versión y los tests viejos dejan de contar hasta que se repitan.</summary>
    public const int Version = 1;
    public const int MinutosEstimados = 2;

    /// <summary>Por debajo de esto, «no coinciden en nada» y no se muestran (aunque coincidan en tiempo).</summary>
    public const int AfinidadMinima = 35;

    /// <summary>La compatibilidad final: 60 % el test y 40 % el tiempo libre que comparten.</summary>
    public const double PesoTest = 0.6;

    public const int MaximoEnComun = 3;

    public const string TipoUnica = "UNICA";
    public const string TipoMultiple = "MULTIPLE";

    public sealed record Opcion(string Id, string Texto, string Resumen);

    /// <param name="Ordinal">En las de una sola respuesta: si las opciones van en escala, la vecina vale la mitad.</param>
    public sealed record Pregunta(
        string Id, string Bloque, string Texto, string Tipo, int Maximo, bool Opcional, double Peso,
        IReadOnlyList<Opcion> Opciones, bool Ordinal = false);

    private static Opcion O(string id, string texto, string resumen) => new(id, texto, resumen);

    public static readonly IReadOnlyList<Pregunta> Preguntas =
    [
        new("planes", "Tus gustos", "¿Qué harías con una hora libre?", TipoMultiple, 3, false, 1.2,
        [
            O("deporte", "Hacer deporte", "Deporte"),
            O("videojuegos", "Jugar videojuegos", "Videojuegos"),
            O("leer", "Leer algo que me guste", "Lectura"),
            O("musica", "Escuchar o tocar música", "Música"),
            O("series", "Ver series o anime", "Series y anime"),
            O("comer", "Salir a comer algo", "Salir a comer"),
            O("crear", "Dibujar, diseñar o crear", "Lo creativo"),
            O("mesa", "Juegos de mesa o cartas", "Juegos de mesa"),
            O("caminar", "Caminar y despejarme", "Caminar"),
            O("siesta", "Una siesta, sin culpa", "Las siestas"),
        ]),
        new("temas", "Tus gustos", "¿De qué podrías hablar horas?", TipoMultiple, 3, false, 1.0,
        [
            O("tecnologia", "Tecnología", "Tecnología"),
            O("cine", "Cine y series", "Cine"),
            O("actualidad", "Actualidad y política", "Actualidad"),
            O("ciencia", "Ciencia", "Ciencia"),
            O("futbol", "Fútbol y deportes", "Fútbol"),
            O("moda", "Moda y estilo", "Moda"),
            O("negocios", "Negocios y emprendimiento", "Emprender"),
            O("viajes", "Viajes", "Viajes"),
            O("memes", "Memes e internet", "Memes"),
            O("historia", "Historia y cultura", "Historia y cultura"),
        ]),
        new("musica", "Tus gustos", "¿Qué suena en tus audífonos?", TipoMultiple, 3, false, 0.7,
        [
            O("urbano", "Reguetón y urbano", "Música urbana"),
            O("rock", "Rock", "Rock"),
            O("pop", "Pop", "Pop"),
            O("folklore", "Folklore boliviano", "Folklore"),
            O("electronica", "Electrónica", "Electrónica"),
            O("indie", "Indie y alternativo", "Indie"),
            O("rap", "Rap y hip hop", "Rap"),
            O("clasica", "Clásica o instrumental", "Música clásica"),
            O("cumbia", "Cumbia", "Cumbia"),
            O("kpop", "K-pop", "K-pop"),
        ]),
        new("grupo", "Tu forma de ser", "En un grupo nuevo, tú…", TipoUnica, 1, false, 0.8,
        [
            O("rompo", "Rompo el hielo", "Romper el hielo"),
            O("observo", "Observo y después me suelto", "Agarrar confianza de a poco"),
            O("espero", "Espero a que me hablen", "Ir con calma al inicio"),
        ], Ordinal: true),
        new("ideal", "Tu forma de ser", "Tu puente ideal es…", TipoUnica, 1, false, 1.0,
        [
            O("muchos", "Con un grupo grande", "Planes en grupo"),
            O("pocos", "Con una o dos personas", "Planes de a pocos"),
            O("solo", "Solo, recargando pilas", "Recargar pilas"),
        ], Ordinal: true),
        new("confianza", "Cómo conectas", "¿Qué te hace sentir en confianza con alguien?", TipoMultiple, 2, false, 1.2,
        [
            O("humor", "Reírnos de lo mismo", "El mismo humor"),
            O("profundo", "Conversaciones profundas", "Hablar en serio"),
            O("hacer", "Hacer cosas juntos", "Hacer cosas juntos"),
            O("apoyo", "Ayudarnos con la U", "Apoyarse en la U"),
            O("comida", "Compartir comida", "Compartir comida"),
            O("escuchar", "Que me escuchen sin juzgar", "Escuchar sin juzgar"),
        ]),
        new("busco", "Cómo conectas", "¿Qué te gustaría encontrar aquí?", TipoMultiple, 3, false, 1.0,
        [
            O("estudio", "Compañeros para estudiar", "Estudiar juntos"),
            O("amistad", "Amistades nuevas", "Amistades nuevas"),
            O("deporte", "Gente para hacer deporte", "Hacer deporte juntos"),
            O("almuerzo", "Compañía para almorzar", "Almorzar juntos"),
            O("proyectos", "Equipo para proyectos", "Armar proyectos"),
            O("charla", "Alguien con quién conversar", "Conversar"),
        ]),
        new("energia", "Tu día a día", "¿Cuándo tienes más energía?", TipoUnica, 1, false, 0.5,
        [
            O("manana", "En la mañana", "Ser de mañana"),
            O("tarde", "En la tarde", "Ser de tarde"),
            O("noche", "De noche", "Ser nocturnos"),
        ], Ordinal: true),
        new("estudio", "Tu día a día", "¿Cómo estudias mejor?", TipoUnica, 1, false, 0.7,
        [
            O("silencio", "En silencio total", "Estudiar en silencio"),
            O("musica", "Con música", "Estudiar con música"),
            O("grupo", "En grupo, explicando", "Estudiar en grupo"),
            O("presion", "A último momento, con presión", "Estudiar bajo presión"),
        ]),
        new("valores", "Valores y creencias", "¿Qué valoras más en una amistad?", TipoMultiple, 2, true, 1.0,
        [
            O("honestidad", "Honestidad", "Honestidad"),
            O("lealtad", "Lealtad", "Lealtad"),
            O("respeto", "Respeto", "Respeto"),
            O("empatia", "Empatía", "Empatía"),
            O("responsabilidad", "Responsabilidad", "Responsabilidad"),
            O("espacio", "Respetar los espacios", "Respetar los espacios"),
        ]),
        new("creencias", "Valores y creencias", "¿Con qué te identificas más?", TipoUnica, 1, true, 0.8,
        [
            O("catolico", "Católico o católica practicante", "Fe católica"),
            O("creyente", "Creyente a mi manera", "Creer a su manera"),
            O("espiritual", "Espiritual, sin religión", "Espiritualidad"),
            // «Otra religión» en las dos no significa la misma: cuenta para la afinidad, pero no se muestra en común.
            O("otra", "Otra religión", ""),
            O("agnostico", "Agnóstico o agnóstica", "Agnosticismo"),
            O("ateo", "Ateo o atea", "Ateísmo"),
        ]),
    ];

    private static readonly Dictionary<string, Pregunta> PorId = Preguntas.ToDictionary(p => p.Id);

    /// <summary>
    /// Revisa y normaliza las respuestas: solo preguntas y opciones del test, sin repetir, entre 1 y el máximo de cada
    /// pregunta (exactamente 1 en las de una respuesta). Las opcionales pueden faltar o venir vacías. Lanza
    /// <see cref="ArgumentException"/> con un mensaje para el estudiante si algo no cuadra.
    /// </summary>
    public static Dictionary<string, List<string>> Validar(IReadOnlyDictionary<string, List<string>?>? respuestas)
    {
        if (respuestas is null) throw new ArgumentException("Faltan las respuestas del test.");

        foreach (var id in respuestas.Keys)
            if (!PorId.ContainsKey(id)) throw new ArgumentException($"La pregunta «{id}» no es parte del test.");

        var limpias = new Dictionary<string, List<string>>();
        foreach (var p in Preguntas)
        {
            var elegidas = respuestas.TryGetValue(p.Id, out var r) && r is not null
                ? r.Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()).ToList()
                : [];

            if (elegidas.Count == 0)
            {
                if (p.Opcional) continue;
                throw new ArgumentException($"Responde: «{p.Texto}»");
            }

            if (elegidas.Distinct().Count() != elegidas.Count)
                throw new ArgumentException($"Repetiste una opción en «{p.Texto}».");
            var desconocida = elegidas.FirstOrDefault(o => p.Opciones.All(x => x.Id != o));
            if (desconocida is not null)
                throw new ArgumentException($"«{desconocida}» no es una opción de «{p.Texto}».");
            if (p.Tipo == TipoUnica && elegidas.Count != 1)
                throw new ArgumentException($"Elige una sola opción en «{p.Texto}».");
            if (elegidas.Count > p.Maximo)
                throw new ArgumentException($"Elige hasta {p.Maximo} opciones en «{p.Texto}».");

            limpias[p.Id] = elegidas;
        }

        return limpias;
    }

    public sealed record Resultado(int Afinidad, IReadOnlyList<string> EnComun);

    /// <summary>
    /// Qué tan parecidas son dos personas según el test (0–100) y lo que más las conecta.
    /// · Varias respuestas: con al menos una en común vale 0,4 + 0,6 × Dice (una en común de 3 y 3 ya es 0,6; tres, 1).
    /// · Una respuesta: igual vale 1; en las de escala, la vecina vale 0,5; distinta, 0.
    /// · Una pregunta solo cuenta si las dos personas la respondieron (las opcionales pueden faltar).
    /// «En común» junta, de las preguntas de más peso a las de menos, las opciones compartidas (hasta 3).
    /// </summary>
    public static Resultado Comparar(IReadOnlyDictionary<string, List<string>> mias, IReadOnlyDictionary<string, List<string>> suyas)
    {
        double suma = 0, pesos = 0;
        var enComun = new List<(double Peso, int Orden, string Texto)>();

        for (var i = 0; i < Preguntas.Count; i++)
        {
            var p = Preguntas[i];
            if (!mias.TryGetValue(p.Id, out var a) || a.Count == 0) continue;
            if (!suyas.TryGetValue(p.Id, out var b) || b.Count == 0) continue;

            var compartidas = a.Intersect(b).ToList();
            double parecido;
            if (p.Tipo == TipoMultiple)
            {
                var dice = 2.0 * compartidas.Count / (a.Count + b.Count);
                parecido = compartidas.Count == 0 ? 0 : 0.4 + 0.6 * dice;
            }
            else if (compartidas.Count == 1) parecido = 1;
            else if (p.Ordinal && Math.Abs(Indice(p, a[0]) - Indice(p, b[0])) == 1) parecido = 0.5;
            else parecido = 0;

            suma += p.Peso * parecido;
            pesos += p.Peso;
            enComun.AddRange(compartidas.Select(o => (p.Peso, i, p.Opciones.First(x => x.Id == o).Resumen)));
        }

        var afinidad = pesos == 0 ? 0 : (int)Math.Round(100 * suma / pesos, MidpointRounding.AwayFromZero);
        var resumen = enComun
            .OrderByDescending(e => e.Peso).ThenBy(e => e.Orden)
            .Select(e => e.Texto).Where(t => t.Length > 0).Distinct().Take(MaximoEnComun).ToList();
        return new Resultado(Math.Clamp(afinidad, 0, 100), resumen);
    }

    /// <summary>Compatibilidad final que se muestra: mezcla el test y el tiempo libre compartido (los dos de 0 a 100).</summary>
    public static int Compatibilidad(int afinidad, int porcentajeTiempo) =>
        (int)Math.Round(PesoTest * afinidad + (1 - PesoTest) * porcentajeTiempo, MidpointRounding.AwayFromZero);

    /// <summary>Se muestra solo si coinciden en las dos cosas: tiempo (eso lo filtra el matching) y test.</summary>
    public static bool Coinciden(Resultado r) => r.Afinidad >= AfinidadMinima && r.EnComun.Count > 0;

    private static int Indice(Pregunta p, string opcion) => p.Opciones.ToList().FindIndex(o => o.Id == opcion);
}
