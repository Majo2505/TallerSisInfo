using Breaku.Application;
using Breaku.Presentation.Rendimiento;
using Breaku.Presentation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Tests;

public class HealthReadyTests
{
    private sealed class BaseFalsa(bool conecta) : IDatabaseHealthCheck
    {
        public Task<bool> CanConnectAsync(CancellationToken cancellationToken) => Task.FromResult(conecta);
    }

    private sealed class BaseLenta : IDatabaseHealthCheck
    {
        public async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return true;
        }
    }

    [Fact]
    public async Task Ready_responde_200_si_la_base_conecta()
    {
        var controller = new HealthController(new BaseFalsa(true), new ApiMetrics());

        var resultado = await controller.GetReady(CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, Assert.IsType<OkObjectResult>(resultado).StatusCode);
    }

    [Fact]
    public async Task Ready_responde_503_si_la_base_no_conecta()
    {
        var controller = new HealthController(new BaseFalsa(false), new ApiMetrics());

        var resultado = await controller.GetReady(CancellationToken.None);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, Assert.IsType<ObjectResult>(resultado).StatusCode);
    }

    [Fact]
    public async Task Ready_responde_503_si_la_base_no_contesta_a_tiempo()
    {
        var controller = new HealthController(new BaseLenta(), new ApiMetrics());

        var resultado = await controller.GetReady(CancellationToken.None);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, Assert.IsType<ObjectResult>(resultado).StatusCode);
    }
}
