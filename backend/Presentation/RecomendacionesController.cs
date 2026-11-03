using System.Security.Claims;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Breaku.Application.Recomendaciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

/// <summary>
/// Recomendación inteligente con asistente RAG de contexto estricto (HU7). El asistente solo puede recomendar
/// lugares reales del catálogo recuperados para el puente; cada recomendación queda en el log de auditoría.
/// </summary>
[ApiController]
[Authorize]
[Route("api/recomendaciones")]
[Tags("Recomendaciones")]
[Produces("application/json")]
public class RecomendacionesController : ControllerBase
{
    private readonly RecomendacionService _service;
    public RecomendacionesController(RecomendacionService service) => _service = service;

    // El usuario sale SIEMPRE del token (claim sub / NameIdentifier), nunca del body ni de la URL.
    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    /// <summary>Pide al asistente recomendaciones para un puente del usuario.</summary>
    /// <remarks>
    /// El asistente <b>solo</b> recibe los lugares del catálogo que cumplen tiempo, cercanía y zona (búsqueda de HU6),
    /// y el sistema descarta cualquier lugar o dato que no salga de ese contexto. Distancia, tiempo y categoría
    /// salen del catálogo; del asistente solo sale <c>explicacion</c>.
    ///
    /// Si no hay lugares viables, no se llama al asistente y se responde <c>200</c> con
    /// <c>estado</c> = <c>SIN_RESULTADOS</c> y el mensaje «No encontramos opciones cercanas para este tiempo disponible.».
    ///
    /// Ejemplo de petición:
    /// <code>
    /// { "puenteId": 5, "entrada": "Estoy cansado, quiero algo tranquilo" }
    /// </code>
    /// </remarks>
    /// <param name="req">Puente y preferencia opcional del estudiante.</param>
    /// <response code="200">Recomendaciones (<c>OK</c>) o mensaje de que no hay opciones (<c>SIN_RESULTADOS</c>).</response>
    /// <response code="400">La preferencia supera los 300 caracteres (<c>codigo</c> = <c>VALIDACION</c>) o el cuerpo está mal formado.</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="404">El puente no existe o no pertenece al usuario (<c>codigo</c> = <c>NO_ENCONTRADO</c>).</response>
    /// <response code="503">El asistente no respondió (<c>codigo</c> = <c>ASISTENTE_NO_DISPONIBLE</c>). La consulta queda registrada como <c>ERROR</c>.</response>
    [HttpPost]
    [ProducesResponseType(typeof(RespuestaRecomendacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> Recomendar(SolicitudRecomendacionDto req, CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.RecomendarAsync(UsuarioId, req, ct)));

    /// <summary>Valora una recomendación con me gusta (<c>LIKE</c>) o no me sirve (<c>DISLIKE</c>) (HU9).</summary>
    /// <remarks>
    /// El voto queda asociado a la recomendación y, a través de su consulta, al usuario del token: solo se pueden valorar
    /// las recomendaciones propias. Hay <b>una sola valoración por recomendación</b>: votar de nuevo reemplaza la anterior
    /// (no se duplica) y repetir el mismo voto no cambia nada. Las valoraciones se ven en las métricas del panel de administración.
    ///
    /// Ejemplo de petición:
    /// <code>
    /// { "feedback": "LIKE" }
    /// </code>
    /// </remarks>
    /// <param name="id">Id de la recomendación (viene en cada recomendación de <c>POST /api/recomendaciones</c>).</param>
    /// <param name="req">La valoración.</param>
    /// <param name="ct">Cancelación de la petición.</param>
    /// <response code="200">Valoración guardada: la vigente y su fecha.</response>
    /// <response code="400">El voto no es <c>LIKE</c> ni <c>DISLIKE</c> (<c>codigo</c> = <c>VALIDACION</c>).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="404">La recomendación no existe o no es tuya (<c>codigo</c> = <c>NO_ENCONTRADO</c>).</response>
    [HttpPatch("{id:long}/feedback")]
    [ProducesResponseType(typeof(FeedbackGuardadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Feedback(long id, FeedbackRecomendacionDto req, CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.GuardarFeedbackAsync(UsuarioId, id, req.Feedback, ct)));

    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> accion)
    {
        try { return await accion(); }
        catch (ReglaNegocioException ex)
        {
            var body = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo switch
            {
                "NO_ENCONTRADO" => NotFound(body),
                "ASISTENTE_NO_DISPONIBLE" => StatusCode(StatusCodes.Status503ServiceUnavailable, body),
                _ => BadRequest(body)
            };
        }
    }
}
