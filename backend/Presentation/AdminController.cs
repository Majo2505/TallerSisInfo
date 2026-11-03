using System.Security.Claims;
using Breaku.Application.Admin;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("api/admin")]
[Tags("Administración")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly AdminService _service;
    public AdminController(AdminService service) => _service = service;

    private int AdminId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    [HttpGet("resumen")]
    [ProducesResponseType(typeof(ResumenAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Resumen(CancellationToken ct) => Ok(await _service.ResumenAsync(ct));

    [HttpGet("lugares")]
    [ProducesResponseType(typeof(List<LugarAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Lugares(CancellationToken ct) => Ok(await _service.ListarLugaresAsync(ct));

    [HttpPatch("lugares/{id:int}/estado")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CambiarEstado(int id, CambioEstadoLugarDto req, CancellationToken ct)
    {
        try
        {
            await _service.CambiarEstadoLugarAsync(id, req, AdminId, ct);
            return NoContent();
        }
        catch (ReglaNegocioException ex)
        {
            var body = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo == "NO_ENCONTRADO" ? NotFound(body) : BadRequest(body);
        }
    }

    [HttpGet("reportes")]
    [ProducesResponseType(typeof(List<ReporteAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Reportes(CancellationToken ct) => Ok(await _service.ListarReportesAsync(ct));

    [HttpPatch("reportes/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResolverReporte(int id, ResolverReporteDto req, CancellationToken ct)
    {
        try
        {
            await _service.ResolverReporteAsync(id, req, AdminId, ct);
            return NoContent();
        }
        catch (ReglaNegocioException ex)
        {
            var body = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo switch
            {
                "NO_ENCONTRADO" => NotFound(body),
                "YA_REVISADO" => Conflict(body),
                _ => BadRequest(body)
            };
        }
    }

    [HttpGet("auditoria-rag")]
    [ProducesResponseType(typeof(List<ConsultaRagAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AuditoriaRag([FromQuery] int? limite, CancellationToken ct)
        => Ok(await _service.ListarConsultasRagAsync(limite, ct));

    [HttpGet("metricas")]
    [ProducesResponseType(typeof(MetricasAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Metricas(CancellationToken ct) => Ok(await _service.MetricasAsync(ct));
}
