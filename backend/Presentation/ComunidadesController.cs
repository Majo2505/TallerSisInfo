using System.Security.Claims;
using Breaku.Application.Comunidades;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/comunidades")]
[Tags("Comunidades")]
[Produces("application/json")]
public class ComunidadesController : ControllerBase
{
    private readonly ComunidadService _service;
    private readonly DescubrirComunidadesService _descubrir;

    public ComunidadesController(ComunidadService service, DescubrirComunidadesService descubrir)
    {
        _service = service;
        _descubrir = descubrir;
    }

    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    [HttpGet]
    [ProducesResponseType(typeof(ResultadoDescubrirDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        try
        {
            return Ok(await _descubrir.ListarAsync(UsuarioId, ct));
        }
        catch (ReglaNegocioException ex) when (ex.Codigo == "NO_ENCONTRADO")
        {
            return NotFound(new ErrorNegocioDto(ex.Codigo, ex.Message));
        }
    }

    [HttpPost("{id:int}/invitaciones")]
    [ProducesResponseType(typeof(InvitacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Invitar(int id, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.InvitarDeNuevoAsync(UsuarioId, id, ct));
        }
        catch (ReglaNegocioException ex)
        {
            var body = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo == "NO_ENCONTRADO" ? NotFound(body) : BadRequest(body);
        }
    }
}
