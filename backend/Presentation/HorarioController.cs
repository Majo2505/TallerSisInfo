using System.Security.Claims;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/horario")]
[Tags("Horario")]
[Produces("application/json")]
public class HorarioController : ControllerBase
{
    private readonly HorarioService _service;
    public HorarioController(HorarioService service) => _service = service;

    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    [HttpGet]
    [ProducesResponseType(typeof(List<HorarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<HorarioDto>>> Listar(CancellationToken ct)
        => Ok(await _service.ListarAsync(UsuarioId, ct));

    [HttpPost]
    [ProducesResponseType(typeof(HorarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Crear(GuardarHorarioRequest req, CancellationToken ct)
        => Ejecutar(async () => CreatedAtAction(nameof(Listar), await _service.CrearAsync(UsuarioId, req, ct)));

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(HorarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Editar(int id, GuardarHorarioRequest req, CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.EditarAsync(UsuarioId, id, req, ct)));

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Eliminar(int id, CancellationToken ct)
        => Ejecutar(async () => { await _service.EliminarAsync(UsuarioId, id, ct); return NoContent(); });

    [HttpPatch("{id:int}/asistencia")]
    [ProducesResponseType(typeof(HorarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public Task<IActionResult> CambiarAsistencia(int id, CambiarAsistenciaRequest req, CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.CambiarAsistenciaAsync(UsuarioId, id, req.Asiste, req.AutorizacionCarrera, ct)));

    [HttpPatch("asistencia")]
    [ProducesResponseType(typeof(List<HorarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public Task<IActionResult> CambiarAsistencias(CambiarAsistenciasRequest req, CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.CambiarAsistenciasAsync(UsuarioId, req.Bloques, ct)));

    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> accion)
    {
        try { return await accion(); }
        catch (ReglaNegocioException ex)
        {
            var body = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo switch
            {
                "DUPLICADO" => Conflict(body),
                "NO_ENCONTRADO" => NotFound(body),
                _ => BadRequest(body)
            };
        }
    }
}
