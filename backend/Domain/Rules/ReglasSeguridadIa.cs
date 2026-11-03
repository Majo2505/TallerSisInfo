using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Breaku.Domain.Rules;

/// <summary>Guardrails de entrada para el asistente de recomendaciones (HU17).</summary>
public static class ReglasSeguridadIa
{
    public const string CategoriaInyeccion = "INYECCION";
    public const string CategoriaFueraDeAlcance = "FUERA_DE_ALCANCE";
    public const string CategoriaBienestar = "BIENESTAR";
    public const string CategoriaFueraDeReglamento = "FUERA_DE_REGLAMENTO";

    public const string MensajeInyeccion = "La solicitud intenta modificar las instrucciones del asistente.";
    public const string MensajeFueraDeAlcance = "Solo puedo recomendar actividades y lugares del campus para tu puente.";
    public const string MensajeBienestar = "Si estás pasando por un momento difícil, habla con alguien de confianza o con el área de bienestar de tu universidad.";
    public const string MensajeFueraDeReglamento = "Solo puedo recomendar actividades académicas y respetuosas, dentro del reglamento de la universidad.";

    /// <summary>Por qué se rechazó una entrada: la categoría sirve para el registro y el mensaje se le muestra al estudiante.</summary>
    public sealed record Rechazo(string Categoria, string Mensaje);

    private static readonly TimeSpan TiempoMaximo = TimeSpan.FromSeconds(1);

    private static Regex Patron(string expresion)
        => new(expresion, RegexOptions.CultureInvariant, TiempoMaximo);

    // «DAN» solo cuenta en mayúsculas: «dan» es una palabra común («lugares que dan comida»).
    private static readonly Regex Dan = new(@"\bDAN\b", RegexOptions.CultureInvariant, TiempoMaximo);

    private static readonly Regex[] Inyeccion =
    [
        // Ordenes para cambiar o saltarse las reglas del asistente.
        Patron(@"\b(ignor\w*|olvid\w*|omit\w*|saltar\w*|saltate|desobedec\w*|desobedez\w*|disregard|forget|bypass|override|cambi\w*|reemplaz\w*|sustitu\w*|anul\w*|deshabilit\w*|desactiv\w*|quit\w*|elimin\w*)\b.{0,80}\b(previous|prior|above|anterior\w*|sistema|system|developer|desarrollador|prompt|instruccion\w*|instruction\w*|regla\w*|rule\w*|restriccion\w*|restriction\w*|filtro\w*|limite\w*|guardrail\w*|configuracion|politica\w*|seguridad)\b"),
        // Intentos de que el asistente revele su prompt o sus instrucciones.
        Patron(@"\b(revel\w*|muestr\w*|mostr\w*|dime|dame|dinos|explic\w*|repit\w*|imprim\w*|copi\w*|ensen\w*|comparte|pasame|escrib\w*|list\w*|cual es|cuales son|cual fue)\b.{0,40}\b(prompt|system prompt|mensaje del sistema|instrucciones (de|del|internas?|iniciales|originales)|configuracion interna|reglas internas|tus instrucciones|tus reglas|tu configuracion)\b"),
        // Cambios de rol o «modos» sin restricciones.
        Patron(@"\b(actua\w* como|actuar como|finge\w*|fingir|haz de cuenta|hazte pasar|simula\w* (ser|que)|comportate como|eres ahora|ahora eres|a partir de ahora (eres|seras|actua)|pretend\w* (you|to)|you are now|act as|roleplay|juego de roles|modo (desarrollador|dios|developer|sin restricciones|libre)|developer mode|jailbreak|do anything now|sin (restricciones|filtros|censura)|unrestricted|uncensored)\b")
    ];

    private static readonly Regex[] FueraDeAlcance =
    [
        Patron(@"\b(contrasena\w*|password\w*|passwd|clave (de |del )?(api|acceso|admin\w*|secreta)|api ?key|apikey|credenciales de (acceso|admin\w*|usuarios?|otros?)|jwt|token (de acceso|jwt|api|secreto|de sesion)|codigo fuente|source code|datos personales|informacion personal|datos privados|prompt del sistema|variables? de entorno|connection ?string)\b")
    ];

    private static readonly Regex[] Bienestar =
    [
        Patron(@"\b(suicid\w*|autolesion\w*|quitarme la vida|hacerme dano|cortarme las venas)\b")
    ];

    private static readonly Regex[] FueraDeReglamento =
    [
        // Alcohol y vida nocturna.
        Patron(@"\b(alcohol\w*|cerveza\w*|chela\w*|birra\w*|vodka|whisk\w*|tequila|ron|pisco|singani|fernet|licor\w*|trago\w*|chupar\w*|borrach\w*|ebri\w*|embriag\w*|curda|resaca|coctel\w*|cocktail\w*|happy hour|vino (tinto|blanco|espumante)|vinoteca|copas? de vino|botellas? de vino|tomar vino|beber vino|discoteca\w*|boliche\w*|antro\w*|pub|rumba\w*|farra|parranda|juerga)\b"),
        // Sustancias no lícitas y tabaco.
        Patron(@"\b(droga\w*|drogad\w*|marihuana|mariguana|cannabis|weed|porro\w*|mota|cocaina|crack|extasis|lsd|ketamina|heroina|opio|anfetamina\w*|metanfetamina\w*|alucinogen\w*|narcotic\w*|estupefaciente\w*|pasta base|sustancias? (ilicit\w*|ilegal\w*|prohibid\w*|psicoactiv\w*)|fumar\w*|fumo|fumando|cigarr\w*|tabaco|vape\w*|shisha|hookah|narguile)\b"),
        // Juegos de azar.
        Patron(@"\b(apuest\w*|apostar\w*|casino\w*|tragamonedas|ruleta|blackjack|poker|juegos? de azar)\b"),
        // Contenido e insinuaciones sexuales.
        Patron(@"\b(sexo|sexual\w*|sexy|sexi|erotic\w*|porn\w*|desnud\w*|prostitu\w*|escort|sensual\w*|lenceri\w*|burdel|cabaret|table dance|strip ?tease|motel|telo|swinger|besar\w*|besos?|ligar\w*|ligue|coquete\w*|seduc\w*|seducir|manosea\w*|toquete\w*|acostarme con|dormir juntos|hacer el amor|tener relaciones|relaciones intimas|citas? romantic\w*)\b"),
        // Ocultismo y actividades paranormales.
        Patron(@"\b(paranormal\w*|fantasma\w*|ouija|ouiya|brujeri\w*|brujo\w*|bruja\w*|hechiz\w*|embrujad\w*|poltergeist|exorci\w*|demoni\w*|demonio\w*|satan\w*|diablo\w*|ocultis\w*|esoteric\w*|espiritismo|espiritist\w*|invocar\w*|invocacion\w*|sesion(es)? espirita(s)?|tarot|horoscop\w*|astrologi\w*|vidente\w*|adivin\w*|magia negra|maldicion\w*|aparecidos?|almas? en pena|hablar con (los )?(muertos|espiritus|difuntos)|ritual\w* (oscur\w*|satanic\w*|magic\w*)|ovnis?|extraterrestre\w*)\b"),
        // Violencia, delitos y deshonestidad académica.
        Patron(@"\b(matar(?! (el )?tiempo)\w*|asesin\w*|golpear\w*|pelea\w* callejera\w*|pelearme|armas? de fuego|pistola\w*|cuchillo\w*|explosiv\w*|bomba\w*|terroris\w*|secuestr\w*|robar\w*|hurtar|estafar\w*|extorsi\w*|hackear\w*|hackeo|piratear\w*|plagi\w*|hacer trampa|copiar(me)? en (el |un |los |las )?(examen\w*|prueba\w*|parcial\w*)|chuleta\w*|fraude\w*|falsific\w*|comprar (un |el |mi )?(examen|titulo|tesis|notas)|suplantar\w*)\b"),
        // Lenguaje vulgar o irrespetuoso.
        Patron(@"\b(mierda|puta|puto|putas|putos|carajo|verga|pendej\w*|cabron\w*|malparid\w*|hijueputa|hdp|gilipollas|imbecil\w*|idiota\w*|estupid\w*|maricon\w*|marica|lacra|culiao|boludo|huevon\w*|weon\w*)\b")
    ];

    /// <summary>
    /// Pasa el texto a minúsculas y sin tildes, vuelve a unir las letras separadas («i g n o r a»), cambia los números
    /// que sustituyen letras («t0das») y deja solo letras, números y espacios simples. Así «Ignorá» y «contrasena» se detectan igual.
    /// </summary>
    public static string Normalizar(string texto)
    {
        var sinTildes = new StringBuilder(texto.Length);
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            sinTildes.Append(char.ToLowerInvariant(c));
        }

        var t = sinTildes.ToString().Normalize(NormalizationForm.FormC);
        t = Regex.Replace(t, @"[a-z0-9@$]+", m => MezclaLetrasYSimbolos(m.Value) ? Destransformar(m.Value) : m.Value, RegexOptions.None, TiempoMaximo);
        t = Regex.Replace(t, @"\b(?:[a-z][\s.\-_*]+){3,}[a-z]\b", m => Regex.Replace(m.Value, @"[\s.\-_*]+", ""), RegexOptions.None, TiempoMaximo);
        t = Regex.Replace(t, @"[^a-z0-9]+", " ", RegexOptions.None, TiempoMaximo);
        return t.Trim();
    }

    private static bool MezclaLetrasYSimbolos(string palabra)
        => palabra.Any(char.IsAsciiLetter) && palabra.Any(c => c is '0' or '1' or '3' or '4' or '5' or '7' or '@' or '$');

    private static string Destransformar(string palabra)
    {
        var sb = new StringBuilder(palabra.Length);
        foreach (var c in palabra)
            sb.Append(c switch { '0' => 'o', '1' => 'i', '3' => 'e', '4' => 'a', '5' => 's', '7' => 't', '@' => 'a', '$' => 's', _ => c });
        return sb.ToString();
    }

    /// <summary>
    /// Quita lo que permitiría cerrar el bloque de la preferencia en el prompt (los signos &lt; y &gt;) y los caracteres de control.
    /// </summary>
    public static string Sanear(string entrada)
    {
        var sb = new StringBuilder(entrada.Length);
        foreach (var c in entrada)
        {
            if (c is '<' or '>') sb.Append(' ');
            else if (char.IsControl(c)) sb.Append(' ');
            else sb.Append(c);
        }
        return Regex.Replace(sb.ToString(), @"\s+", " ", RegexOptions.None, TiempoMaximo).Trim();
    }

    /// <summary>Devuelve el rechazo si la entrada no se puede mandar al asistente, o <c>null</c> si se puede.</summary>
    public static Rechazo? Evaluar(string? entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada)) return null;

        try
        {
            if (Dan.IsMatch(entrada)) return new Rechazo(CategoriaInyeccion, MensajeInyeccion);

            var texto = Normalizar(entrada);
            if (Inyeccion.Any(p => p.IsMatch(texto))) return new Rechazo(CategoriaInyeccion, MensajeInyeccion);
            if (FueraDeAlcance.Any(p => p.IsMatch(texto))) return new Rechazo(CategoriaFueraDeAlcance, MensajeFueraDeAlcance);
            if (Bienestar.Any(p => p.IsMatch(texto))) return new Rechazo(CategoriaBienestar, MensajeBienestar);
            if (FueraDeReglamento.Any(p => p.IsMatch(texto))) return new Rechazo(CategoriaFueraDeReglamento, MensajeFueraDeReglamento);
            return null;
        }
        catch (RegexMatchTimeoutException)
        {
            return new Rechazo(CategoriaInyeccion, MensajeInyeccion);
        }
    }

    /// <summary>El mensaje del rechazo, o <c>null</c> si la entrada se puede enviar.</summary>
    public static string? MotivoRechazo(string? entrada) => Evaluar(entrada)?.Mensaje;
}
