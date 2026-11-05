using Breaku.Application.Comunidades;
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
        await ArchivarAsync(stoppingToken);
        using var timer = new PeriodicTimer(Intervalo);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await ArchivarAsync(stoppingToken);
    }

    private async Task ArchivarAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ICierreComunidadesService>();
            var cantidad = await service.ArchivarVencidasAsync(DateTime.UtcNow, ct);
            if (cantidad > 0)
                _logger.LogInformation("Comunidades archivadas automáticamente: {Cantidad}", cantidad);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "No se pudieron archivar las comunidades vencidas.");
        }
    }
}
