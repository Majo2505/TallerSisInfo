using System.Security.Claims;
using Breaku.Application.Admin;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

/// <summary>
/// Panel de administración del campus: cifras, catálogo de lugares, reportes y auditoría del asistente RAG.
/// Solo para usuarios con rol <c>ADMIN</c> (claim <c>role</c> del token).
/// </summary>
[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("api/admin")]
[Tags("Administración")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly AdminService _service;
    public AdminController(AdminService service) => _service = service;

    // El administrador sale SIEMPRE del token, nunca del body ni de la URL.
    private int AdminId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    /// <summary>Cifras del campus: lugares activos y en revisión, estudiantes, comunidades de hoy, consultas al asistente de la semana y reportes pendientes.</summary>
    /// <response code="200">Resumen del campus.</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="403">El usuario no es administrador.</response>
    [HttpGet("resumen")]
    [ProducesResponseType(typeof(ResumenAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Resumen(CancellationToken ct) => Ok(await _service.ResumenAsync(ct));

    /// <summary>Catálogo completo de lugares (primero los que esperan revisión) con cuántas veces se recomendó cada uno.</summary>
    /// <response code="200">Lugares del catálogo.</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="403">El usuario no es administrador.</response>
    [HttpGet("lugares")]
    [ProducesResponseType(typeof(List<LugarAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Lugares(CancellationToken ct) => Ok(await _service.ListarLugaresAsync(ct));

    /// <summary>Cambia el estado de un lugar. Solo los lugares <c>ACTIVO</c> entran en las recomendaciones.</summary>
    /// <remarks>
    /// El cambio queda en <c>historial_cambio</c> con el estado anterior, el nuevo y el administrador que lo hizo.
    ///
    /// Ejemplo de petición:
    /// <code>
    /// { "estado": "INACTIVO" }
    /// </code>
    /// </remarks>
    /// <response code="204">Estado actualizado.</response>
    /// <response code="400">El estado no es <c>ACTIVO</c>, <c>EN_REVISION</c> ni <c>INACTIVO</c> (<c>codigo</c> = <c>VALIDACION</c>).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="403">El usuario no es administrador.</response>
    /// <response code="404">El lugar no existe (<c>codigo</c> = <c>NO_ENCONTRADO</c>).</response>
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

    /// <summary>Reportes de estudiantes sobre lugares o comunidades (primero los pendientes; hasta 100).</summary>
    /// <response code="200">Reportes.</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="403">El usuario no es administrador.</response>
    [HttpGet("reportes")]
    [ProducesResponseType(typeof(List<ReporteAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Reportes(CancellationToken ct) => Ok(await _service.ListarReportesAsync(ct));

    /// <summary>Resuelve o descarta un reporte pendiente y registra al administrador que lo revisó (HU16).</summary>
    /// <remarks>
    /// Un reporte se revisa una sola vez: queda <c>RESUELTO</c> o <c>DESCARTADO</c>, con el administrador del token como
    /// <c>revisor_id</c> y la fecha en <c>revisado_en</c>. La fila se bloquea mientras se revisa, así que si dos
    /// administradores lo intentan a la vez, el segundo recibe <c>YA_REVISADO</c>.
    ///
    /// Ejemplo de petición:
    /// <code>
    /// { "estado": "RESUELTO" }
    /// </code>
    /// </remarks>
    /// <param name="id">Id del reporte (de <c>GET /api/admin/reportes</c>).</param>
    /// <param name="req">La decisión.</param>
    /// <param name="ct">Cancelación de la petición.</param>
    /// <response code="204">Reporte revisado.</response>
    /// <response code="400">El estado no es <c>RESUELTO</c> ni <c>DESCARTADO</c> (<c>codigo</c> = <c>VALIDACION</c>).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="403">El usuario no es administrador.</response>
    /// <response code="404">El reporte no existe (<c>codigo</c> = <c>NO_ENCONTRADO</c>).</response>
    /// <response code="409">El reporte ya fue revisado (<c>codigo</c> = <c>YA_REVISADO</c>).</response>
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

    /// <summary>Últimas consultas al asistente RAG con su entrada, estado, modelo y latencia (auditoría).</summary>
    /// <param name="limite">Cuántas devolver: 1 a 200 (por defecto 50).</param>
    /// <response code="200">Consultas, de la más reciente a la más antigua.</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="403">El usuario no es administrador.</response>
    [HttpGet("auditoria-rag")]
    [ProducesResponseType(typeof(List<ConsultaRagAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AuditoriaRag([FromQuery] int? limite, CancellationToken ct)
        => Ok(await _service.ListarConsultasRagAsync(limite, ct));

    /// <summary>Métricas del asistente en los últimos 7 días: consultas por resultado, latencia promedio, lugares más recomendados y valoraciones de los estudiantes (HU9).</summary>
    /// <response code="200">Métricas.</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="403">El usuario no es administrador.</response>
    [HttpGet("metricas")]
    [ProducesResponseType(typeof(MetricasAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Metricas(CancellationToken ct) => Ok(await _service.MetricasAsync(ct));
}
