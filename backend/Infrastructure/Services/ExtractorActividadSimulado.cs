using System.Text.Json;
using System.Text.RegularExpressions;
using Breaku.Application.Actividades;
using Breaku.Application.Recomendaciones;
using Breaku.Domain.Rules;

namespace Breaku.Infrastructure.Services;

public class ExtractorActividadSimulado : IExtractorActividad
{
    public const string NombreModelo = "simulado";

    private static readonly TimeSpan TiempoMaximo = TimeSpan.FromSeconds(1);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly Dictionary<string, int> NumerosEnPalabras = new()
    {
        ["un"] = 1, ["una"] = 1, ["uno"] = 1, ["dos"] = 2, ["tres"] = 3, ["cuatro"] = 4, ["cinco"] = 5, ["seis"] = 6,
        ["siete"] = 7, ["ocho"] = 8, ["nueve"] = 9, ["diez"] = 10, ["once"] = 11, ["doce"] = 12, ["quince"] = 15,
        ["veinte"] = 20, ["treinta"] = 30, ["cuarenta"] = 40
    };

    private const string Numero = @"(\d{1,3}|un|una|uno|dos|tres|cuatro|cinco|seis|siete|ocho|nueve|diez|once|doce|quince|veinte|treinta|cuarenta)";

    private static readonly (Regex Patron, string Nombre, int? Cupo)[] Conocidas =
    [
        (Patron(@"\bcartas?\b|\bnaipes?\b|\bcacho\b"), "jugar cartas", 4),
        (Patron(@"\bajedrez\b"), "jugar ajedrez", 2),
        (Patron(@"\bfulbito\b|\bfutbol\b|\bfutsal\b"), "jugar fútbol", 10),
        (Patron(@"\bbasquet\w*|\bbasket\w*|\bbaloncesto\b"), "jugar básquet", 10),
        (Patron(@"\bvoley\w*|\bvolei\w*"), "jugar vóley", 12),
        (Patron(@"\bping ?pong\b|\btenis de mesa\b"), "jugar ping pong", 4),
        (Patron(@"\bcafe\w*\b"), "tomar un café", null),
        (Patron(@"\balmorz\w*|\balmuerzo\b"), "almorzar", null),
        (Patron(@"\bcaminar\b|\bcaminata\b|\bpasear\b"), "caminar", null)
    ];

    private static readonly Regex VerboConObjeto =
        Patron(@"\b(jugar|estudiar|repasar|practicar|tomar|comer|caminar|leer|ver|hacer|armar|charlar|conversar)\b(?: (?:un|una|el|la|los|las|unos|unas))?(?: (?!por\b|en\b|a\b|con\b|para\b|hasta\b|de\b|y\b|durante\b)(\w+))?");

    private static readonly Regex Ahora = Patron(@"\b(ahora|ahorita|ya mismo|de una)\b");
    private static readonly Regex EnMinutos = Patron(@"\ben " + Numero + @" (min|mins|minuto|minutos)\b");
    private static readonly Regex EnMediaHora = Patron(@"\ben media hora\b");
    private static readonly Regex EnHoras = Patron(@"\ben " + Numero + @" horas?\b");
    private static readonly Regex ALaUna = Patron(@"\ba la una\b( y media)?");
    private static readonly Regex ALas = Patron(@"\ba las (\d{1,2})(?: (\d{2}))?( y media)?( de la tarde| de la noche| pm| de la manana| am)?\b");

    private static readonly Regex HoraYMedia = Patron(@"(?<!\ben )\b(?:" + Numero + @" )?horas? y media\b");
    private static readonly Regex Horas = Patron(@"(?<!\ben )\b" + Numero + @" horas?\b");
    private static readonly Regex MediaHora = Patron(@"(?<!\ben )\bmedia hora\b");
    private static readonly Regex Minutos = Patron(@"(?<!\ben )\b" + Numero + @" (min|mins|minuto|minutos)\b");

    private static readonly Regex ConAmigos = Patron(@"\bcon (otros? |otras? )?" + Numero + @" (amig\w*|compa\w*|personas?|mas|chicos|chicas)\b");
    private static readonly Regex CupoTotal = Patron(@"\b(?:somos|seriamos|hasta|maximo|max) " + Numero + @"(?: personas?| participantes?| jugadores?)?\b");
    private static readonly Regex Personas = Patron(@"\b" + Numero + @" (personas|participantes|jugadores)\b");

    private static Regex Patron(string expresion) => new(expresion, RegexOptions.CultureInvariant, TiempoMaximo);

    public Task<RespuestaExtractor> ExtraerAsync(PromptRag prompt, string texto, CancellationToken ct = default)
    {
        var argumentos = Extraer(texto);
        return Task.FromResult(new RespuestaExtractor(NombreModelo, JsonSerializer.Serialize(argumentos, Json), argumentos));
    }

    public static ArgumentosExtraidos Extraer(string texto)
    {
        var separado = Regex.Replace(texto, @"(?<=\d)(?=\p{L})|(?<=\p{L})(?=\d)", " ", RegexOptions.None, TiempoMaximo);
        var n = ReglasSeguridadIa.Normalizar(separado);
        var (actividad, cupoHabitual) = LeerActividad(n);
        var (hora, enMinutos) = LeerInicio(n);
        var participantes = LeerParticipantes(n) ?? cupoHabitual;

        return new ArgumentosExtraidos(actividad is not null, actividad, hora, enMinutos, LeerDuracion(n), participantes);
    }

    private static (string? Actividad, int? Cupo) LeerActividad(string n)
    {
        foreach (var (patron, nombre, cupo) in Conocidas)
            if (patron.IsMatch(n)) return (nombre, cupo);

        var verbo = VerboConObjeto.Match(n);
        if (!verbo.Success) return (null, null);
        return (verbo.Groups[2].Success ? $"{verbo.Groups[1].Value} {verbo.Groups[2].Value}" : verbo.Groups[1].Value, null);
    }

    private static (string? Hora, int? EnMinutos) LeerInicio(string n)
    {
        if (EnMinutos.Match(n) is { Success: true } m) return (null, Valor(m.Groups[1].Value));
        if (EnMediaHora.IsMatch(n)) return (null, 30);
        if (EnHoras.Match(n) is { Success: true } h) return (null, Valor(h.Groups[1].Value) * 60);

        if (ALaUna.Match(n) is { Success: true } una) return (una.Groups[1].Success ? "13:30" : "13:00", null);
        if (ALas.Match(n) is { Success: true } a)
        {
            var hora = int.Parse(a.Groups[1].Value);
            var minutos = a.Groups[2].Success ? int.Parse(a.Groups[2].Value) : a.Groups[3].Success ? 30 : 0;
            var sufijo = a.Groups[4].Value.Trim();
            var esTarde = sufijo is "de la tarde" or "de la noche" or "pm";
            var esManana = sufijo is "de la manana" or "am";
            if (hora < 12 && (esTarde || (!esManana && hora * 60 + minutos < 7 * 60 + 15))) hora += 12;
            if (hora > 23 || minutos > 59) return (null, null);
            return ($"{hora:00}:{minutos:00}", null);
        }

        if (Ahora.IsMatch(n)) return (null, 0);
        return (null, null);
    }

    private static int? LeerDuracion(string n)
    {
        if (HoraYMedia.Match(n) is { Success: true } hm) return (hm.Groups[1].Success ? Valor(hm.Groups[1].Value) : 1) * 60 + 30;
        if (Horas.Match(n) is { Success: true } h) return Valor(h.Groups[1].Value) * 60;
        if (MediaHora.IsMatch(n)) return 30;
        if (Minutos.Match(n) is { Success: true } m) return Valor(m.Groups[1].Value);
        return null;
    }

    private static int? LeerParticipantes(string n)
    {
        if (ConAmigos.Match(n) is { Success: true } c) return Valor(c.Groups[2].Value) + 1;
        if (CupoTotal.Match(n) is { Success: true } t) return Valor(t.Groups[1].Value);
        if (Personas.Match(n) is { Success: true } p) return Valor(p.Groups[1].Value);
        return null;
    }

    private static int Valor(string numero) => int.TryParse(numero, out var v) ? v : NumerosEnPalabras[numero];
}
