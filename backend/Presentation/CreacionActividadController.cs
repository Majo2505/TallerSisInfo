using System.Security.Claims;
using Breaku.Application.Actividades;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

/// <summary>
/// Creación de la actividad (HU10): el estudiante revisó el resumen de <c>POST /api/actividades/extraer</c> y lo confirma.
/// Todos los endpoints requieren JWT y el usuario sale siempre del token.
/// </summary>
[ApiController]
[Authorize]
[Route("api/actividades")]
[Tags("Actividades")]
[Produces("application/json")]
public class CreacionActividadController : ControllerBase
{
    private readonly CreacionActividadService _service;
    public CreacionActividadController(CreacionActividadService service) => _service = service;

    // El usuario sale SIEMPRE del token (claim sub / NameIdentifier), nunca del body ni de la URL.
    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    /// <summary>Crea la actividad que el estudiante confirmó.</summary>
    /// <remarks>
    /// Se manda lo que dejó la extracción completa (<c>actividad</c>, <c>horaInicio</c>, <c>duracionMin</c>, <c>maxParticipantes</c>), el texto
    /// original y su <c>consultaId</c>. El servidor vuelve a validar todo con las reglas de la extracción, comprueba que la consulta sea del
    /// propio usuario y esté completa, y la guarda como <c>log_id</c> de la comunidad. Una misma consulta no se puede usar dos veces.
    /// Si el estudiante no confirma, no se llama a este endpoint y no se crea nada.
    /// </remarks>
    /// <param name="req">Datos confirmados.</param>
    /// <param name="ct">Cancelación de la petición.</param>
    /// <response code="201">Actividad creada.</response>
    /// <response code="400">Datos inválidos o consulta inexistente o incompleta (<c>VALIDACION</c>), o texto que no pasa los guardrails (<c>SOLICITUD_NO_PERMITIDA</c>).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="409">Esa consulta ya se usó para crear una actividad (<c>DUPLICADO</c>).</response>
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
