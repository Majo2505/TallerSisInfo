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

    [HttpPatch("lugares/{id:int}/validacion")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CambiarValidacion(int id, CambioValidacionLugarDto req, CancellationToken ct)
    {
        try
        {
            await _service.CambiarValidacionLugarAsync(id, req, AdminId, ct);
            return NoContent();
        }
        catch (ReglaNegocioException ex)
        {
            var body = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo == "NO_ENCONTRADO" ? NotFound(body) : BadRequest(body);
        }
    }

    /// <summary>Categorías del catálogo (para el formulario de lugar).</summary>
    [HttpGet("categorias")]
    [ProducesResponseType(typeof(List<CategoriaAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Categorias(CancellationToken ct) => Ok(await _service.ListarCategoriasAsync(ct));

    /// <summary>Campus activos con su centro y su radio: un lugar tiene que quedar dentro de alguno.</summary>
    [HttpGet("campus")]
    [ProducesResponseType(typeof(List<CampusAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Campus(CancellationToken ct) => Ok(await _service.ListarCampusAsync(ct));

    /// <summary>Un lugar con todos sus datos editables.</summary>
    /// <response code="200">El lugar.</response>
    /// <response code="404">El lugar no existe (<c>NO_ENCONTRADO</c>).</response>
    [HttpGet("lugares/{id:int}")]
    [ProducesResponseType(typeof(LugarDetalleAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Lugar(int id, CancellationToken ct)
        => Responder(async () => Ok(await _service.ObtenerLugarAsync(id, ct)));

    /// <summary>Crea un lugar en el catálogo. Queda activo y validado, y se puede recomendar.</summary>
    /// <remarks>
    /// El lugar tiene que quedar dentro del radio de un campus activo (<c>GET /api/admin/campus</c>), con un nombre único,
    /// una duración de 1 a 240 minutos y, si tiene horario, apertura y cierre (el cierre después). Queda en el historial.
    /// </remarks>
    /// <response code="201">Lugar creado (<c>{ "id": … }</c>).</response>
    /// <response code="400">Algún dato no es válido (<c>VALIDACION</c>; el mensaje dice cuál).</response>
    /// <response code="409">Ya hay un lugar con ese nombre (<c>NOMBRE_REPETIDO</c>).</response>
    [HttpPost("lugares")]
    [ProducesResponseType(typeof(LugarCreadoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public Task<IActionResult> CrearLugar(GuardarLugarDto req, CancellationToken ct)
        => Responder(async () => StatusCode(StatusCodes.Status201Created, await _service.CrearLugarAsync(req, AdminId, ct)));

    /// <summary>Edita un lugar del catálogo (mismas reglas que al crearlo). Queda el antes y el después en el historial.</summary>
    /// <response code="204">Lugar actualizado.</response>
    /// <response code="400">Algún dato no es válido (<c>VALIDACION</c>).</response>
    /// <response code="404">El lugar no existe (<c>NO_ENCONTRADO</c>).</response>
    /// <response code="409">Ya hay otro lugar con ese nombre (<c>NOMBRE_REPETIDO</c>).</response>
    [HttpPut("lugares/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public Task<IActionResult> EditarLugar(int id, GuardarLugarDto req, CancellationToken ct)
        => Responder(async () => { await _service.EditarLugarAsync(id, req, AdminId, ct); return NoContent(); });

    /// <summary>Elimina un lugar que nunca se usó.</summary>
    /// <remarks>
    /// Si el lugar ya aparece en recomendaciones, reportes o comunidades, borrarlo rompería ese historial: responde 409
    /// <c>EN_USO</c> y hay que desactivarlo (<c>PATCH /api/admin/lugares/{id}/estado</c>). El borrado queda en el historial.
    /// </remarks>
    /// <response code="204">Lugar eliminado.</response>
    /// <response code="404">El lugar no existe (<c>NO_ENCONTRADO</c>).</response>
    /// <response code="409">El lugar ya se usó (<c>EN_USO</c>).</response>
    [HttpDelete("lugares/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public Task<IActionResult> EliminarLugar(int id, CancellationToken ct)
        => Responder(async () => { await _service.EliminarLugarAsync(id, AdminId, ct); return NoContent(); });

    private async Task<IActionResult> Responder(Func<Task<IActionResult>> accion)
    {
        try
        {
            return await accion();
        }
        catch (ReglaNegocioException ex)
        {
            var body = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo switch
            {
                "NO_ENCONTRADO" => NotFound(body),
                "NOMBRE_REPETIDO" or "EN_USO" => Conflict(body),
                _ => BadRequest(body)
            };
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
