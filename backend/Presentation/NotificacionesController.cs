using System.Security.Claims;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Breaku.Application.Notificaciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notificaciones")]
[Tags("Notificaciones")]
[Produces("application/json")]
public class NotificacionesController : ControllerBase
{
    private readonly NotificacionService _service;
    public NotificacionesController(NotificacionService service) => _service = service;

    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    [HttpGet]
    [ProducesResponseType(typeof(List<NotificacionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<NotificacionDto>>> Listar(CancellationToken ct)
        => Ok(await _service.ListarAsync(UsuarioId, ct));

    [HttpPatch("{id:long}/leida")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public Task<IActionResult> MarcarLeida(long id, CancellationToken ct)
        => Ejecutar(async () => { await _service.MarcarLeidaAsync(UsuarioId, id, ct); return NoContent(); });

    [HttpGet("configuracion")]
    [ProducesResponseType(typeof(ConfiguracionNotificacionesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> ObtenerConfiguracion(CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.ObtenerConfiguracionAsync(UsuarioId, ct)));

    [HttpPut("configuracion")]
    [ProducesResponseType(typeof(ConfiguracionNotificacionesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ConfiguracionNotificacionesDto>> GuardarConfiguracion(ConfiguracionNotificacionesDto req, CancellationToken ct)
        => Ok(await _service.GuardarConfiguracionAsync(UsuarioId, req, ct));

    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> accion)
    {
        try { return await accion(); }
        catch (ReglaNegocioException ex) when (ex.Codigo == "NO_ENCONTRADO")
        {
            return NotFound(new ErrorNegocioDto(ex.Codigo, ex.Message));
        }
    }
}
