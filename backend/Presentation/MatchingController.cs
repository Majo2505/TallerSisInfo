using System.Security.Claims;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Breaku.Application.Matching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/matching")]
[Tags("Matching")]
[Produces("application/json")]
public class MatchingController : ControllerBase
{
    private readonly MatchingService _service;
    public MatchingController(MatchingService service) => _service = service;

    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    [HttpGet]
    [ProducesResponseType(typeof(ResultadoMatchingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Buscar([FromQuery] int puenteId, CancellationToken ct)
    {
        if (puenteId <= 0)
            return Task.FromResult<IActionResult>(BadRequest(new ErrorNegocioDto("VALIDACION", "Indica el puente con el parámetro puenteId.")));
        return Ejecutar(async () => Ok(await _service.BuscarAsync(UsuarioId, puenteId, ct)));
    }

    [HttpGet("configuracion")]
    [ProducesResponseType(typeof(ConfiguracionMatchingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public Task<IActionResult> ObtenerConfiguracion(CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.ObtenerConfiguracionAsync(UsuarioId, ct)));

    [HttpPut("configuracion")]
    [ProducesResponseType(typeof(ConfiguracionMatchingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public Task<IActionResult> ActualizarConfiguracion(ActualizarConfiguracionMatchingRequest req, CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.ActualizarConfiguracionAsync(UsuarioId, req, ct)));

    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> accion)
    {
        try { return await accion(); }
        catch (ReglaNegocioException ex)
        {
            var body = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo == "NO_ENCONTRADO" ? NotFound(body) : BadRequest(body);
        }
    }
}
