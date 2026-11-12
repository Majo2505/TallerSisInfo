using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Breaku.Application.Eventos;
using Breaku.Domain.Rules;

namespace Breaku.Infrastructure.Services;

public static class LectorEventosInstagram
{
    private static readonly TimeSpan DuracionPorDefecto = TimeSpan.FromHours(2);
    private static readonly TimeSpan MaximoPasadoSinAnio = TimeSpan.FromDays(30);

    private static readonly Regex CaptionRegex = new(
        "\\\\?\"(?:edge_media_to_caption|caption)\\\\?\".*?\\\\?\"text\\\\?\"\\s*:\\s*\\\\?\"(?<text>(?:\\\\.|[^\"\\\\])*)",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex FechaRegex = new(
        @"(?<day>\d{1,2})(?:[/-](?<month>\d{1,2})|(?:\s+de\s+|\s+)(?<monthName>enero|febrero|marzo|abril|mayo|junio|julio|agosto|septiembre|setiembre|octubre|noviembre|diciembre))(?:[/-](?<year>\d{4})|\s+de\s+(?<yearName>\d{4}))?.*?(?<start>\d{1,2}:\d{2})(?:\s*(?:a|-)\s*(?<end>\d{1,2}:\d{2}))?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly IReadOnlyDictionary<string, int> Meses = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["enero"] = 1, ["febrero"] = 2, ["marzo"] = 3, ["abril"] = 4,
        ["mayo"] = 5, ["junio"] = 6, ["julio"] = 7, ["agosto"] = 8,
        ["septiembre"] = 9, ["setiembre"] = 9, ["octubre"] = 10,
        ["noviembre"] = 11, ["diciembre"] = 12
    };

    public static IReadOnlyList<EventoImportado> Extraer(string html, string urlPerfil, DateTime ahoraUtc)
    {
        var eventos = new Dictionary<string, EventoImportado>();
        foreach (Match match in CaptionRegex.Matches(html))
        {
            var caption = DecodificarCaption(match.Groups["text"].Value);
            var evento = Interpretar(caption, urlPerfil, ahoraUtc);
            if (evento is not null) eventos[evento.FuenteUrl] = evento;
        }
        return eventos.Values.ToList();
    }

    public static EventoImportado? Interpretar(string caption, string urlPerfil, DateTime ahoraUtc)
    {
        var fecha = FechaRegex.Match(caption);
        if (!fecha.Success) return null;

        var ahoraLocal = ReglasNotificacion.AHoraLocal(ahoraUtc);
        var mes = fecha.Groups["month"].Success ? int.Parse(fecha.Groups["month"].Value) : Meses[fecha.Groups["monthName"].Value];
        var dia = int.Parse(fecha.Groups["day"].Value);
        var anioExplicito = fecha.Groups["year"].Success || fecha.Groups["yearName"].Success;
        var anio = fecha.Groups["year"].Success ? int.Parse(fecha.Groups["year"].Value)
            : fecha.Groups["yearName"].Success ? int.Parse(fecha.Groups["yearName"].Value)
            : ahoraLocal.Year;

        var inicio = ConstruirFechaHora(anio, mes, dia, fecha.Groups["start"].Value);
        if (inicio is null) return null;
        if (!anioExplicito && inicio.Value < ahoraLocal - MaximoPasadoSinAnio)
            inicio = ConstruirFechaHora(anio + 1, mes, dia, fecha.Groups["start"].Value);
        if (inicio is null) return null;

        var fin = fecha.Groups["end"].Success
            ? ConstruirFechaHora(inicio.Value.Year, inicio.Value.Month, inicio.Value.Day, fecha.Groups["end"].Value)
            : null;
        if (fin is null || fin <= inicio) fin = inicio.Value + DuracionPorDefecto;

        var titulo = caption.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(linea => linea.Trim())
            .FirstOrDefault(linea => linea.Length > 0) ?? "Evento universitario";
        if (titulo.Length > 200) titulo = titulo[..200];
        var descripcion = caption.Length > 2000 ? caption[..2000] : caption;

        return new EventoImportado(
            $"{urlPerfil}#evento-{Huella(titulo, inicio.Value)}",
            titulo,
            descripcion,
            DateTime.SpecifyKind(inicio.Value - ReglasNotificacion.DesfaseBolivia, DateTimeKind.Utc),
            DateTime.SpecifyKind(fin.Value - ReglasNotificacion.DesfaseBolivia, DateTimeKind.Utc),
            null);
    }

    private static DateTime? ConstruirFechaHora(int anio, int mes, int dia, string hora)
    {
        if (anio is < 2000 or > 2100 || mes is < 1 or > 12 || dia < 1 || dia > DateTime.DaysInMonth(anio, mes)) return null;
        if (!TimeOnly.TryParseExact(hora, ["H:mm", "HH:mm"], null, System.Globalization.DateTimeStyles.None, out var tiempo)) return null;
        return new DateTime(anio, mes, dia).Add(tiempo.ToTimeSpan());
    }

    private static string DecodificarCaption(string crudo)
    {
        try
        {
            return JsonSerializer.Deserialize<string>($"\"{crudo}\"") ?? crudo;
        }
        catch (JsonException)
        {
            return crudo;
        }
    }

    private static string Huella(string titulo, DateTime inicioLocal)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{titulo.Trim().ToLowerInvariant()}|{inicioLocal:yyyyMMddHHmm}"));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
