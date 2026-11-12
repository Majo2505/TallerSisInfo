using Breaku.Api.Hubs;
using Breaku.Application.Comunidades;
using Breaku.Domain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Breaku.Infrastructure.Services;

public sealed class CierreComunidadesWorker : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(15);
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<CierreComunidadesWorker> _logger;

    public CierreComunidadesWorker(IServiceScopeFactory scopes, ILogger<CierreComunidadesWorker> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await LimpiarAvisosAsync(stoppingToken);
        await ArchivarAsync(stoppingToken);
        using var timer = new PeriodicTimer(Intervalo);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await ArchivarAsync(stoppingToken);
    }

    private async Task LimpiarAvisosAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ICierreComunidadesService>();
            var apagados = await service.LimpiarAvisosDeArchivadasAsync(ct);
            if (apagados > 0)
                _logger.LogInformation("Avisos de comunidades ya archivadas apagados: {Cantidad}", apagados);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "No se pudieron apagar los avisos de las comunidades archivadas.");
        }
    }

    private async Task ArchivarAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ICierreComunidadesService>();
            var hub = scope.ServiceProvider.GetRequiredService<IHubContext<ComunidadHub>>();
            var archivadas = await service.ArchivarVencidasAsync(DateTime.UtcNow, ct);
            if (archivadas.Count == 0) return;

            _logger.LogInformation("Comunidades archivadas automáticamente: {Cantidad}", archivadas.Count);
            foreach (var id in archivadas)
                await hub.Clients.Group(ComunidadHub.Grupo(id)).SendAsync(
                    "comunidadActualizada", new ComunidadEstadoActualizado(id, ComunidadEfimera.EstadoArchivada, 0), ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "No se pudieron archivar las comunidades vencidas.");
        }
    }
}
