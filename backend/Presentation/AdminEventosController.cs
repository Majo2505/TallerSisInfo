using Breaku.Application.DTOs;
using Breaku.Application.Eventos;
using Breaku.Application.Horarios;
using Breaku.Domain;
using Breaku.Domain.Rules;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Presentation.Controllers;

[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("api/admin/eventos")]
[Tags("Administración de eventos")]
[Produces("application/json")]
public sealed class AdminEventosController : ControllerBase
{
    private readonly EventoService _service;

    public AdminEventosController(EventoService service) => _service = service;

    private int AdminId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    [HttpGet("pendientes")]
    [ProducesResponseType(typeof(List<EventoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Pendientes(CancellationToken ct)
        => Ok(await _service.ListarPendientesAsync(ct));

    /// <summary>HU26: eventos publicados que todavía no terminaron, para bajarlos si son inapropiados.</summary>
    [HttpGet("publicados")]
    [ProducesResponseType(typeof(List<EventoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Publicados(CancellationToken ct)
        => Ok(await _service.ListarPublicadosAdminAsync(ct));

    /// <summary>HU26: historial de eventos rechazados en revisión o bajados, con motivo, fecha y admin. Solo lectura.</summary>
    [HttpGet("rechazados")]
    [ProducesResponseType(typeof(List<EventoRechazadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Rechazados(CancellationToken ct)
        => Ok(await _service.ListarRechazadosAsync(ct));

    /// <summary>
    /// Aprobar o rechazar un evento pendiente. Con <c>cambios</c>, el admin lo edita y lo aprueba en un solo paso.
    /// </summary>
    [HttpPatch("{id:int}/moderacion")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Moderar(int id, ResolverEventoRequest request, CancellationToken ct)
        => Ejecutar(() => _service.ModerarAsync(id, AdminId, request, ct));

    /// <summary>
    /// HU26: bajar un evento publicado. Queda RECHAZADO (oculto, no se borra) y el autor recibe un aviso. Con <c>reporteId</c>
    /// (HU38), ese reporte queda RESUELTO y quien reportó recibe REPORTE_RESUELTO, en la misma transacción. No sanciona.
    /// </summary>
    [HttpPatch("{id:int}/bajar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Bajar(int id, BajarEventoRequest request, CancellationToken ct)
        => Ejecutar(() => _service.BajarAsync(id, AdminId, request, ct));

    private async Task<IActionResult> Ejecutar(Func<Task> accion)
    {
        try
        {
            await accion();
            return NoContent();
        }
        catch (ReglaNegocioException ex)
        {
            var error = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo switch
            {
                "NO_ENCONTRADO" => NotFound(error),
                "YA_REVISADO" or "EVENTO_FINALIZADO" or ReglasEvento.CodigoReporteNoCorresponde => Conflict(error),
                _ => BadRequest(error)
            };
        }
    }
}
