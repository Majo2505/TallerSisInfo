using System.Security.Claims;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Breaku.Application.Recomendaciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/recomendaciones")]
[Tags("Recomendaciones")]
[Produces("application/json")]
public class RecomendacionesController : ControllerBase
{
    private readonly RecomendacionService _service;
    public RecomendacionesController(RecomendacionService service) => _service = service;

    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    [HttpPost]
    [ProducesResponseType(typeof(RespuestaRecomendacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> Recomendar(SolicitudRecomendacionDto req, CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.RecomendarAsync(UsuarioId, req, ct)));

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
