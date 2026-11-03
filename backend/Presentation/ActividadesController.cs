using System.Security.Claims;
using Breaku.Application.Actividades;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/actividades")]
[Tags("Actividades")]
[Produces("application/json")]
public class ActividadesController : ControllerBase
{
    private readonly ExtraccionActividadService _service;
    public ActividadesController(ExtraccionActividadService service) => _service = service;

    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    [HttpPost("extraer")]
    [ProducesResponseType(typeof(RespuestaExtraccionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> Extraer(SolicitudExtraccionDto req, CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.ExtraerAsync(UsuarioId, req, ct)));

    [HttpPost("revisar")]
    [ProducesResponseType(typeof(RespuestaExtraccionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> Revisar(RevisionActividadDto req, CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.RevisarAsync(UsuarioId, req, ct)));

    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> accion)
    {
        try
        {
            return await accion();
        }
        catch (ReglaNegocioException ex)
        {
            var body = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo == "ASISTENTE_NO_DISPONIBLE"
                ? StatusCode(StatusCodes.Status503ServiceUnavailable, body)
                : BadRequest(body);
        }
    }
}
