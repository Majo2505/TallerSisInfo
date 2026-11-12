using Breaku.Application.Eventos;

namespace Breaku.Infrastructure.Services;

public sealed class InstagramEventosClient : IEventosPublicosClient
{
    private readonly HttpClient _http;
    private readonly TimeProvider _reloj;
    private readonly string _profileUrl;

    public InstagramEventosClient(HttpClient http, IConfiguration configuration) : this(http, configuration, TimeProvider.System) { }

    public InstagramEventosClient(HttpClient http, IConfiguration configuration, TimeProvider reloj)
    {
        _http = http;
        _reloj = reloj;
        _profileUrl = configuration["INSTAGRAM_EVENTS_URL"]
            ?? "https://www.instagram.com/lacatocbba/?hl=es";
    }

    public async Task<IReadOnlyList<EventoImportado>> ObtenerAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync(_profileUrl, ct);
        if (!response.IsSuccessStatusCode) return [];
        var html = await response.Content.ReadAsStringAsync(ct);
        return LectorEventosInstagram.Extraer(html, _profileUrl, _reloj.GetUtcNow().UtcDateTime);
    }
}
