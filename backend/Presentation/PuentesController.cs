using System.Security.Claims;
using Breaku.Application.Puentes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/puentes")]
[Tags("Puentes")]
[Produces("application/json")]
public class PuenteController : ControllerBase
{
    private readonly PuenteService _service;
    public PuenteController(PuenteService service) => _service = service;

    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    [HttpGet]
    [ProducesResponseType(typeof(List<PuenteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<PuenteDto>>> Listar(CancellationToken ct)
        => Ok(await _service.ListarAsync(UsuarioId, ct));
}
