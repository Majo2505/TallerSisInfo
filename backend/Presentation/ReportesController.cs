using System.Security.Claims;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Breaku.Application.Reportes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

/// <summary>
/// Reportes de los estudiantes (Perfil → Ajustes → «Cuéntanos qué pasó»). Quedan pendientes y los revisa un administrador
/// en el panel (<c>GET /api/admin/reportes</c>).
/// </summary>
[ApiController]
[Authorize]
[Route("api/reportes")]
[Tags("Reportes")]
[Produces("application/json")]
public class ReportesController : ControllerBase
{
    private readonly ReporteService _service;
    public ReportesController(ReporteService service) => _service = service;

    // El autor sale SIEMPRE del token.
    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    /// <summary>Lugares activos que se pueden reportar (para el selector del formulario).</summary>
    /// <response code="200">Lugares, por nombre.</response>
    [HttpGet("lugares")]
    [ProducesResponseType(typeof(List<LugarReportableDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Lugares(CancellationToken ct) => Ok(await _service.ListarLugaresAsync(ct));

    /// <summary>Envía un reporte.</summary>
    /// <remarks>
    /// <c>ERROR_APP</c> y <c>SUGERENCIA</c> van sin lugar; <c>LUGAR_INCORRECTO</c> lleva <c>lugarId</c>. La descripción tiene de
    /// 10 a 500 caracteres. Un mismo lugar no se reporta otra vez mientras siga pendiente, y cada estudiante tiene como mucho
    /// 3 reportes pendientes.
    ///
    /// Ejemplo de petición:
    /// <code>
    /// { "motivo": "LUGAR_INCORRECTO", "lugarId": 4, "descripcion": "El horario dice que abre a las 7, pero abre a las 9." }
    /// </code>
    /// </remarks>
    /// <response code="201">Reporte recibido (queda <c>PENDIENTE</c>).</response>
    /// <response code="400">Falta un dato o no es válido (<c>VALIDACION</c>; el mensaje dice cuál).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="404">El lugar no existe (<c>NO_ENCONTRADO</c>).</response>
    /// <response code="409">Ya reportaste ese lugar y sigue pendiente (<c>YA_REPORTADO</c>), o ya tienes 3 pendientes (<c>DEMASIADOS_PENDIENTES</c>).</response>
    [HttpPost]
    [ProducesResponseType(typeof(ReporteCreadoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(NuevoReporteDto req, CancellationToken ct)
    {
        try
        {
            return StatusCode(StatusCodes.Status201Created, await _service.CrearAsync(UsuarioId, req, ct));
        }
        catch (ReglaNegocioException ex)
        {
            var body = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo switch
            {
                "NO_ENCONTRADO" => NotFound(body),
                "YA_REPORTADO" or "DEMASIADOS_PENDIENTES" => Conflict(body),
                _ => BadRequest(body)
            };
        }
    }
}
