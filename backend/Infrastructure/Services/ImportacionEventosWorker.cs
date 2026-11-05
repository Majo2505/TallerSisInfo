using Breaku.Application.Eventos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Breaku.Infrastructure.Services;

public sealed class ImportacionEventosWorker : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(15);
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ImportacionEventosWorker> _logger;

    public ImportacionEventosWorker(IServiceScopeFactory scopes, ILogger<ImportacionEventosWorker> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ImportarAsync(stoppingToken);
        using var timer = new PeriodicTimer(Intervalo);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await ImportarAsync(stoppingToken);
    }

    private async Task ImportarAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            await scope.ServiceProvider.GetRequiredService<EventosService>().RefrescarAsync(ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "No se pudieron importar eventos públicos de Instagram.");
        }
    }
}
