using System.Security.Claims;
using Breaku.Application.Ubicacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/ubicacion")]
public sealed class UbicacionController : ControllerBase
{
    private readonly UbicacionService _service;

    public UbicacionController(UbicacionService service) => _service = service;

    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    [HttpGet]
    public async Task<ActionResult<EstadoUbicacionDto>> Obtener(CancellationToken ct)
    {
        try
        {
            return Ok(await _service.ObtenerEstadoAsync(UsuarioId, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
    }

    [HttpGet("zonas")]
    public async Task<ActionResult<List<ZonaDto>>> ListarZonas(CancellationToken ct)
        => Ok(await _service.ListarZonasAsync(ct));

    [HttpPut]
    public async Task<ActionResult<EstadoUbicacionDto>> Compartir(
        [FromBody] CompartirUbicacionRequest request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await _service.CompartirAsync(UsuarioId, request, ct));
        }
        catch (UbicacionFueraDeZonaException ex)
        {
            return UnprocessableEntity(new { detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
    }

    [HttpPut("zona")]
    public async Task<ActionResult<EstadoUbicacionDto>> SeleccionarZona(
        [FromBody] SeleccionarZonaRequest request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await _service.SeleccionarZonaAsync(UsuarioId, request.ZonaId, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Revocar(CancellationToken ct)
    {
        try
        {
            await _service.RevocarAsync(UsuarioId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
    }
}
