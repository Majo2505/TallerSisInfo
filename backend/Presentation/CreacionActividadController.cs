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
public class CreacionActividadController : ControllerBase
{
    private readonly CreacionActividadService _service;
    public CreacionActividadController(CreacionActividadService service) => _service = service;

    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    [HttpPost]
    [ProducesResponseType(typeof(ActividadCreadaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(CrearActividadRequest req, CancellationToken ct)
    {
        try
        {
            var creada = await _service.CrearAsync(UsuarioId, req, ct);
            return StatusCode(StatusCodes.Status201Created, creada);
        }
        catch (ReglaNegocioException ex)
        {
            var body = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo == "DUPLICADO" ? Conflict(body) : BadRequest(body);
        }
    }
}
