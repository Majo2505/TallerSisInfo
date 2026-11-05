using System.Text.Json;
using System.Text.RegularExpressions;
using Breaku.Application.Eventos;

namespace Breaku.Infrastructure.Services;

public sealed class InstagramEventosClient : IEventosPublicosClient
{
    private static readonly Regex CaptionRegex = new("\"edge_media_to_caption\".*?\"text\":\"(?<text>.*?)\"", RegexOptions.Compiled);
    private static readonly Regex DateRegex = new(@"(?<day>\d{1,2})[/-](?<month>\d{1,2})(?:[/-](?<year>\d{4}))?.*?(?<start>\d{1,2}:\d{2})(?:\s*(?:a|-)\s*(?<end>\d{1,2}:\d{2}))?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly HttpClient _http;
    private readonly string _profileUrl;

    public InstagramEventosClient(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _profileUrl = configuration["INSTAGRAM_EVENTS_URL"]
            ?? "https://www.instagram.com/lacatocbba/?hl=es";
    }

    public async Task<IReadOnlyList<EventoImportado>> ObtenerAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync(_profileUrl, ct);
        if (!response.IsSuccessStatusCode) return [];
        var html = await response.Content.ReadAsStringAsync(ct);
        var eventos = new List<EventoImportado>();
        foreach (Match match in CaptionRegex.Matches(html))
        {
            var caption = JsonSerializer.Deserialize<string>($"\"{match.Groups["text"].Value}\"") ?? "";
            var fecha = DateRegex.Match(caption);
            if (!fecha.Success) continue;
            var now = DateTime.UtcNow;
            var year = fecha.Groups["year"].Success ? int.Parse(fecha.Groups["year"].Value) : now.Year;
            var inicioLocal = new DateTime(year, int.Parse(fecha.Groups["month"].Value), int.Parse(fecha.Groups["day"].Value))
                .Add(TimeSpan.Parse(fecha.Groups["start"].Value));
            var fin = fecha.Groups["end"].Success
                ? inicioLocal.Date.Add(TimeSpan.Parse(fecha.Groups["end"].Value))
                : inicioLocal.AddHours(2);
            eventos.Add(new EventoImportado(
                $"{_profileUrl}#evento-{inicioLocal:yyyyMMddHHmm}-{eventos.Count}",
                caption.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "Evento universitario",
                caption,
                DateTime.SpecifyKind(inicioLocal - TimeSpan.FromHours(4), DateTimeKind.Utc),
                DateTime.SpecifyKind(fin - TimeSpan.FromHours(4), DateTimeKind.Utc),
                null));
        }
        return eventos;
    }
}
