using System.Security.Claims;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Breaku.Application.Lugares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/lugares")]
[Tags("Lugares")]
[Produces("application/json")]
public class LugaresController : ControllerBase
{
    private readonly BusquedaLugaresService _service;
    public LugaresController(BusquedaLugaresService service) => _service = service;

    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    [HttpGet("viables")]
    [ProducesResponseType(typeof(ResultadoBusquedaLugaresDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Viables([FromQuery] int puenteId, CancellationToken ct)
    {
        if (puenteId <= 0)
            return BadRequest(new ErrorNegocioDto("VALIDACION", "Indica el puente con el parámetro puenteId."));

        try
        {
            return Ok(await _service.BuscarParaPuenteAsync(UsuarioId, puenteId, ct));
        }
        catch (ReglaNegocioException ex)
        {
            return NotFound(new ErrorNegocioDto(ex.Codigo, ex.Message));
        }
    }
}
