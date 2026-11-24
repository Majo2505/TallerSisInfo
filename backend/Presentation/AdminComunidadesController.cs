using System.Security.Claims;
using Breaku.Application.Admin;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Breaku.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Presentation.Controllers;

/// <summary>
/// HU35: el admin quita una comunidad inapropiada (queda BAJADA: no se borra nada y no sanciona a quien la creó) y puede
/// restaurarla. Desde el panel o desde un reporte de esa comunidad (el reporte queda RESUELTO).
/// </summary>
[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("api/admin/comunidades")]
[Tags("Administración")]
[Produces("application/json")]
public sealed class AdminComunidadesController : ControllerBase
{
    private readonly ModeracionComunidadesService _service;

    public AdminComunidadesController(ModeracionComunidadesService service) => _service = service;

    private int AdminId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    /// <summary>Las comunidades ACTIVAS que no terminaron (por inicio) o las BAJADAS (la última quitada primero). Hasta 100.</summary>
    /// <param name="estado"><c>ACTIVA</c> (por defecto) o <c>BAJADA</c>.</param>
    /// <param name="ct">Cancelación de la petición.</param>
    [HttpGet]
    [ProducesResponseType(typeof(List<ComunidadAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> Listar([FromQuery] string? estado, CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.ListarAsync(estado, ct)));

    /// <summary>
    /// Quita una comunidad ACTIVA con un motivo obligatorio. Avisa (COMUNIDAD_BAJADA) a quien la creó y a quienes iban. Con
    /// <c>reporteId</c>, ese reporte queda RESUELTO y quien reportó recibe REPORTE_RESUELTO, en la misma transacción.
    /// </summary>
    [HttpPatch("{id:int}/baja")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Quitar(int id, QuitarComunidadRequest request, CancellationToken ct)
        => Ejecutar(async () =>
        {
            await _service.QuitarAsync(id, AdminId, request, ct);
            return NoContent();
        });

    /// <summary>Restaura una comunidad BAJADA: vuelve a ACTIVA y avisa (COMUNIDAD_RESTAURADA). Si ya terminó, el cierre automático la archiva.</summary>
    [HttpPatch("{id:int}/restauracion")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Restaurar(int id, CancellationToken ct)
        => Ejecutar(async () =>
        {
            await _service.RestaurarAsync(id, ct);
            return NoContent();
        });

    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> accion)
    {
        try
        {
            return await accion();
        }
        catch (ReglaNegocioException ex)
        {
            var error = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo switch
            {
                ReglasModeracionComunidad.ErrorNoEncontrada => NotFound(error),
                ReglasModeracionComunidad.ErrorEstado or ReglasModeracionComunidad.ErrorReporteYaRevisado => Conflict(error),
                _ => BadRequest(error)
            };
        }
    }
}
