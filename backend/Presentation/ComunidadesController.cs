using System.Security.Claims;
using Breaku.Application.Comunidades;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

[ApiController, Authorize, Route("api/comunidades"), Tags("Comunidades")]
public sealed class ComunidadesController(ComunidadService service) : ControllerBase
{
    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    [HttpGet]
    public Task<List<ComunidadDto>> Listar(CancellationToken ct) => service.ListarAsync(ct);

    [HttpPost]
    public async Task<IActionResult> Crear(CrearComunidadRequest request, CancellationToken ct)
    {
        try { return Ok(await service.CrearAsync(UsuarioId, request, ct)); }
        catch (ReglaNegocioException ex) { return BadRequest(new ErrorNegocioDto(ex.Codigo, ex.Message)); }
    }

    [HttpPatch("{id:int}/rsvp")]
    public async Task<IActionResult> Rsvp(int id, RsvpRequest request, CancellationToken ct)
    {
        try { return Ok(await service.ResponderAsync(UsuarioId, id, request.Confirmar, ct)); }
        catch (ReglaNegocioException ex) { return NotFound(new ErrorNegocioDto(ex.Codigo, ex.Message)); }
    }
}

public record RsvpRequest(bool Confirmar);
