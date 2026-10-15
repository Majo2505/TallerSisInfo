using Breaku.Application;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Presentation;

/// <summary>Comprobaciones de estado del servicio. No requieren autenticación y no devuelven cuerpo.</summary>
[ApiController]
[Route("health")]
[Tags("Health")]
public class HealthController : ControllerBase
{
    private readonly IDatabaseHealthCheck _databaseHealthCheck;

    public HealthController(IDatabaseHealthCheck databaseHealthCheck)
    {
        _databaseHealthCheck = databaseHealthCheck;
    }

    /// <summary>Comprueba que la API está en marcha.</summary>
    /// <response code="200">La API responde.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok();

    /// <summary>Comprueba que la API puede conectarse a la base de datos MySQL.</summary>
    /// <response code="200">Conexión a la base de datos correcta.</response>
    /// <response code="503">No se pudo conectar a la base de datos.</response>
    [HttpGet("db")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDb(CancellationToken cancellationToken)
    {
        var connected = await _databaseHealthCheck.CanConnectAsync(cancellationToken);
        return connected ? Ok() : StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}
