using System.Security.Claims;
using Breaku.Application.Actividades;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Breaku.Application.TiempoReal;
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
    private readonly ITiempoReal _tiempoReal;

    public CreacionActividadController(CreacionActividadService service, ITiempoReal tiempoReal)
    {
        _service = service;
        _tiempoReal = tiempoReal;
    }

    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    /// <summary>Crea la actividad que el estudiante confirmó (HU10).</summary>
    /// <remarks>
    /// Se crea el resumen que guardó la extracción (<c>log_auditoria_rag.datos_extraidos</c>), no lo que mande el cliente: si
    /// no coincide, responde 409 <c>RESUMEN_DISTINTO</c>. La extracción tiene que ser del usuario, estar completa y ser de hoy.
    /// </remarks>
    /// <response code="201">Actividad creada (con la invitación automática de HU13).</response>
    /// <response code="400">Datos inválidos o consulta incompleta o de otro día (<c>VALIDACION</c>), o texto que no pasa los guardrails (<c>SOLICITUD_NO_PERMITIDA</c>).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="404">La consulta no existe o es de otro estudiante (<c>NO_ENCONTRADO</c>).</response>
    /// <response code="409">Esa consulta ya se usó (<c>DUPLICADO</c>, también en un doble clic) o los datos no son los del resumen confirmado (<c>RESUMEN_DISTINTO</c>).</response>
    [HttpPost]
    [ProducesResponseType(typeof(ActividadCreadaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(CrearActividadRequest req, CancellationToken ct)
    {
        try
        {
            var creada = await _service.CrearAsync(UsuarioId, req, ct);
            // HU18: aparece en Descubrir de su sede y la invitación llega a los invitados, sin recargar.
            await _tiempoReal.ComunidadCreadaAsync(creada.Id, ct);
            return StatusCode(StatusCodes.Status201Created, creada);
        }
        catch (ReglaNegocioException ex)
        {
            var body = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo switch
            {
                "NO_ENCONTRADO" => NotFound(body),
                "DUPLICADO" or "RESUMEN_DISTINTO" => Conflict(body),
                _ => BadRequest(body)
            };
        }
    }
}
