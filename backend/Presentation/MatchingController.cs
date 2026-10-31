using System.Security.Claims;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Breaku.Application.Matching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

/// <summary>
/// Matching de estudiantes con puente compatible (HU12): compañeros de tu misma sede que están libres al mismo tiempo
/// que tú, ordenados por compatibilidad. Solo aparece quien aceptó aparecer, y para ver a otros hay que aceptarlo también.
/// </summary>
[ApiController]
[Authorize]
[Route("api/matching")]
[Tags("Matching")]
[Produces("application/json")]
public class MatchingController : ControllerBase
{
    private readonly MatchingService _service;
    public MatchingController(MatchingService service) => _service = service;

    // El usuario sale SIEMPRE del token: nadie consulta ni cambia el consentimiento de otro.
    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    /// <summary>Compañeros con tiempo libre compatible con uno de tus puentes.</summary>
    /// <remarks>
    /// <b>Cruce por franja horaria:</b> se cruzan tus puentes con los de los estudiantes de tu misma sede (activa) que aceptaron
    /// aparecer. Dos puentes son compatibles si caen el mismo día y comparten <b>30 minutos o más</b>; se devuelve el tramo en común.
    ///
    /// <b>Orden por compatibilidad:</b> primero quien comparte más minutos contigo; en empate, quien coincide contigo en más
    /// franjas de la semana. <c>porcentaje</c> es qué parte de tu puente cubre el tramo en común.
    ///
    /// No hay compañeros (200 con <c>companeros: []</c>) cuando: no elegiste campus (<c>SIN_ZONA</c>), no aceptaste aparecer
    /// (<c>NO_VISIBLE</c>) o nadie está libre contigo (<c>SIN_RESULTADOS</c>). No se devuelve el correo ni el id de nadie.
    /// </remarks>
    /// <param name="puenteId">Id de uno de tus puentes (de <c>GET /api/puentes</c>).</param>
    /// <param name="ct">Cancelación de la petición.</param>
    /// <response code="200">Resultado del matching; mira <c>estado</c>.</response>
    /// <response code="400">Falta <c>puenteId</c> (<c>VALIDACION</c>).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="404">El puente no existe o no es tuyo (<c>NO_ENCONTRADO</c>).</response>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoMatchingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Buscar([FromQuery] int puenteId, CancellationToken ct)
    {
        if (puenteId <= 0)
            return Task.FromResult<IActionResult>(BadRequest(new ErrorNegocioDto("VALIDACION", "Indica el puente con el parámetro puenteId.")));
        return Ejecutar(async () => Ok(await _service.BuscarAsync(UsuarioId, puenteId, ct)));
    }

    /// <summary>Si aceptaste que compañeros con tiempo compatible te encuentren.</summary>
    /// <param name="ct">Cancelación de la petición.</param>
    /// <response code="200">Consentimiento actual. Por defecto, <c>visible: false</c>.</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="404">La cuenta no existe o está dada de baja (<c>NO_ENCONTRADO</c>).</response>
    [HttpGet("configuracion")]
    [ProducesResponseType(typeof(ConfiguracionMatchingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public Task<IActionResult> ObtenerConfiguracion(CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.ObtenerConfiguracionAsync(UsuarioId, ct)));

    /// <summary>Acepta o retira el consentimiento para aparecer en el matching.</summary>
    /// <remarks>
    /// Al aceptar se guarda la fecha; al retirarlo dejas de aparecer de inmediato y tampoco ves a otros.
    /// El cambio queda en <c>historial_cambio</c>.
    ///
    /// Ejemplo de petición:
    /// <code>
    /// { "visible": true }
    /// </code>
    /// </remarks>
    /// <param name="req">Nuevo valor del consentimiento.</param>
    /// <param name="ct">Cancelación de la petición.</param>
    /// <response code="200">Consentimiento actualizado.</response>
    /// <response code="400">Falta <c>visible</c> (<c>VALIDACION</c>).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="404">La cuenta no existe o está dada de baja (<c>NO_ENCONTRADO</c>).</response>
    [HttpPut("configuracion")]
    [ProducesResponseType(typeof(ConfiguracionMatchingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public Task<IActionResult> ActualizarConfiguracion(ActualizarConfiguracionMatchingRequest req, CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.ActualizarConfiguracionAsync(UsuarioId, req, ct)));

    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> accion)
    {
        try { return await accion(); }
        catch (ReglaNegocioException ex)
        {
            var body = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo == "NO_ENCONTRADO" ? NotFound(body) : BadRequest(body);
        }
    }
}
