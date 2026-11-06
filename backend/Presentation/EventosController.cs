using System.Security.Claims;
using Breaku.Application.Eventos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Presentation;

[ApiController]
[Authorize]
[Route("api/eventos")]
[Tags("Eventos")]
public sealed class EventosController : ControllerBase
{
    private readonly EventosService _service;
    public EventosController(EventosService service) => _service = service;

    [HttpGet("recomendados")]
    public async Task<ActionResult<List<EventoRecomendadoDto>>> Recomendados(CancellationToken ct)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!int.TryParse(claim, out var usuarioId)) return Unauthorized();
        return Ok(await _service.ListarRecomendadosAsync(usuarioId, ct));
    }
}
