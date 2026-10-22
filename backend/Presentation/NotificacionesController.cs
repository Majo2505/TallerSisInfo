using System.Security.Claims;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Breaku.Application.Notificaciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

/// <summary>
/// Avisos de puente disponible (HU4). El servidor crea el aviso solo, en el momento en que empieza cada puente
/// de 45 minutos o más; el cliente lo obtiene consultando <c>GET /api/notificaciones</c>.
/// </summary>
[ApiController]
[Authorize]
[Route("api/notificaciones")]
[Tags("Notificaciones")]
[Produces("application/json")]
public class NotificacionesController : ControllerBase
{
    private readonly NotificacionService _service;
    public NotificacionesController(NotificacionService service) => _service = service;

    // El usuario sale SIEMPRE del token (claim sub / NameIdentifier), nunca del body ni de la URL.
    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    /// <summary>Notificaciones activas del usuario, de la más reciente a la más antigua (máximo 20).</summary>
    /// <remarks>
    /// Cada aviso de puente se crea <b>al empezar el puente</b> (nunca antes, y a más tardar 1 minuto después) y
    /// <b>una sola vez</b> por puente y día. Si el estudiante cambia su horario y el puente deja de existir, su aviso
    /// deja de estar activo y ya no aparece aquí. Con las notificaciones desactivadas no se crean avisos.
    /// No hay push en tiempo real todavía: el cliente debe consultar este endpoint periódicamente (por ejemplo, cada 30 s).
    /// </remarks>
    /// <response code="200">Lista de notificaciones activas (puede ser vacía).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<NotificacionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<NotificacionDto>>> Listar(CancellationToken ct)
        => Ok(await _service.ListarAsync(UsuarioId, ct));

    /// <summary>Marca una notificación como leída.</summary>
    /// <remarks>Es idempotente: marcarla otra vez también responde 204. Si es de otro usuario se responde igual que si no existiera (404).</remarks>
    /// <param name="id" example="31">Identificador de la notificación.</param>
    /// <response code="204">Notificación marcada como leída (sin cuerpo).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="404">La notificación no existe, no está activa o no pertenece al usuario (<c>codigo</c> = <c>NO_ENCONTRADO</c>).</response>
    [HttpPatch("{id:long}/leida")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public Task<IActionResult> MarcarLeida(long id, CancellationToken ct)
        => Ejecutar(async () => { await _service.MarcarLeidaAsync(UsuarioId, id, ct); return NoContent(); });

    /// <summary>Indica si el usuario tiene activadas las notificaciones.</summary>
    /// <response code="200">Preferencia actual. Por defecto, <c>true</c>.</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    [HttpGet("configuracion")]
    [ProducesResponseType(typeof(ConfiguracionNotificacionesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> ObtenerConfiguracion(CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.ObtenerConfiguracionAsync(UsuarioId, ct)));

    /// <summary>Activa o desactiva las notificaciones del usuario.</summary>
    /// <remarks>
    /// Con <c>false</c> no se envía ningún aviso, pero los puentes se siguen detectando (ver <c>GET /api/puentes</c>).
    ///
    /// Ejemplo de petición:
    /// <code>
    /// { "notificacionesActivas": false }
    /// </code>
    /// </remarks>
    /// <param name="req">Nueva preferencia.</param>
    /// <response code="200">Preferencia guardada.</response>
    /// <response code="400">Cuerpo mal formado o sin el campo (<c>ProblemDetails</c> estándar de ASP.NET).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
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
