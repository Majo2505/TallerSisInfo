using Breaku.Application.Notificaciones;
using Breaku.Domain.Rules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Breaku.Infrastructure.Services;

/// <summary>
/// HU4: revisa periódicamente qué puentes acaban de empezar y crea su aviso.
/// El intervalo es menor que la ventana de envío, así ningún puente se queda sin aviso.
/// </summary>
public class AvisoPuentesWorker : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<AvisoPuentesWorker> _logger;

    public AvisoPuentesWorker(IServiceScopeFactory scopes, ILogger<AvisoPuentesWorker> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (Intervalo >= ReglasNotificacion.VentanaEnvio)
            throw new InvalidOperationException("El intervalo del worker debe ser menor que la ventana de envío.");

        using var timer = new PeriodicTimer(Intervalo);
        do
        {
            try
            {
                // El DbContext es scoped: un scope nuevo por cada revisión.
                using var scope = _scopes.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<NotificacionService>();
                var enviados = await service.EnviarAvisosAsync(DateTime.UtcNow, stoppingToken);
                if (enviados > 0) _logger.LogInformation("Avisos de puente enviados: {Cantidad}", enviados);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Un fallo (por ejemplo, la BD caída) no detiene el worker: se reintenta en la próxima vuelta.
                _logger.LogError(ex, "No se pudieron enviar los avisos de puente.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
