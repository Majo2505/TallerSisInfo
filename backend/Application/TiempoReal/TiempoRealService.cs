using Breaku.Application.Comunidades;
using Microsoft.Extensions.Logging;

namespace Breaku.Application.TiempoReal;

/// <summary>
/// HU18/HU20: a quién avisar en vivo. Busca los destinatarios con el repositorio de comunidades y los manda por el
/// <see cref="ICanalEnVivo"/>. Un fallo acá nunca rompe la operación que lo pidió: se registra y sigue.
/// </summary>
public sealed class TiempoRealService : ITiempoReal
{
    private readonly ICanalEnVivo _canal;
    private readonly IComunidadRepository _comunidades;
    private readonly ILogger<TiempoRealService> _logger;

    public TiempoRealService(ICanalEnVivo canal, IComunidadRepository comunidades, ILogger<TiempoRealService> logger)
    {
        _canal = canal;
        _comunidades = comunidades;
        _logger = logger;
    }

    public Task AvisosCambiaronAsync(IReadOnlyCollection<int> usuarioIds, CancellationToken ct = default)
        => usuarioIds.Count == 0
            ? Task.CompletedTask
            : SinRomperAsync(() => _canal.AvisosCambiaronAsync(usuarioIds.Distinct().ToList(), ct), "avisos", ct);

    public async Task ComunidadCreadaAsync(int comunidadId, CancellationToken ct = default)
    {
        await SinRomperAsync(async () =>
        {
            var comunidad = await _comunidades.ObtenerComunidadAsync(comunidadId, ct);
            if (comunidad is null) return;
            var creador = await _comunidades.ObtenerUsuarioAsync(comunidad.CreadorId, ct);
            if (creador?.ZonaActualId is int zonaId)
                await _canal.ComunidadesCambiaronAsync(zonaId, ct);
        }, $"comunidad nueva {comunidadId}", ct);

        await AvisosDeComunidadAsync(comunidadId, ct);
    }

    public Task AvisosDeComunidadAsync(int comunidadId, CancellationToken ct = default)
        => SinRomperAsync(async () =>
        {
            var participantes = await _comunidades.ListarParticipantesAsync(comunidadId, ct);
            if (participantes.Count > 0)
                await _canal.AvisosCambiaronAsync(participantes.Distinct().ToList(), ct);
        }, $"avisos de la comunidad {comunidadId}", ct);

    public Task ComunidadActualizadaAsync(int comunidadId, string estado, int confirmados, CancellationToken ct = default)
        => SinRomperAsync(() => _canal.ComunidadActualizadaAsync(comunidadId, estado, confirmados, ct), $"comunidad {comunidadId}", ct);

    private async Task SinRomperAsync(Func<Task> envio, string que, CancellationToken ct)
    {
        try
        {
            await envio();
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "No se pudo avisar en vivo: {Que}.", que);
        }
    }
}
