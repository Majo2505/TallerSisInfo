using System.Security.Claims;
using Breaku.Application.Puentes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/puentes")]
public class PuenteController : ControllerBase
{
    private readonly PuenteService _service;
    public PuenteController(PuenteService service) => _service = service;

    // El usuario sale SIEMPRE del token (claim sub / NameIdentifier), nunca del body ni de la URL.
    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    /// <summary>Puentes detectados del usuario, ordenados por día y hora. Sin puentes: [].</summary>
    [HttpGet]
    public async Task<ActionResult<List<PuenteDto>>> Listar(CancellationToken ct)
        => Ok(await _service.ListarAsync(UsuarioId, ct));
}