using System.Security.Claims;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Breaku.Application.Lugares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/lugares")]
[Tags("Lugares")]
[Produces("application/json")]
public class LugaresController : ControllerBase
{
    private readonly BusquedaLugaresService _service;
    private readonly FotoLugarService _fotos;

    public LugaresController(BusquedaLugaresService service, FotoLugarService fotos)
    {
        _service = service;
        _fotos = fotos;
    }

    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    [HttpGet("viables")]
    [ProducesResponseType(typeof(ResultadoBusquedaLugaresDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Viables([FromQuery] int puenteId, CancellationToken ct)
    {
        if (puenteId <= 0)
            return BadRequest(new ErrorNegocioDto("VALIDACION", "Indica el puente con el parámetro puenteId."));

        try
        {
            return Ok(await _service.BuscarParaPuenteAsync(UsuarioId, puenteId, ct));
        }
        catch (ReglaNegocioException ex)
        {
            return NotFound(new ErrorNegocioDto(ex.Codigo, ex.Message));
        }
    }

    /// <summary>Foto real de un lugar del catálogo.</summary>
    /// <remarks>
    /// Es pública porque la pide la etiqueta <c>img</c>, que no manda el token; una foto de un lugar del campus no es un
    /// dato privado. Usa la ruta que viene en <c>fotoUrl</c> (con <c>?v=</c>): con la versión, el navegador la guarda un año
    /// y solo la vuelve a pedir cuando el administrador la cambia.
    /// </remarks>
    /// <response code="200">La imagen (JPG, PNG o WebP).</response>
    /// <response code="404">El lugar no tiene foto o no existe (<c>NO_ENCONTRADO</c>).</response>
    [AllowAnonymous]
    [HttpGet("{id:int}/foto")]
    [Produces("image/jpeg", "image/png", "image/webp", "application/json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Foto(int id, CancellationToken ct)
    {
        try
        {
            var foto = await _fotos.ObtenerAsync(id, ct);
            Response.Headers.CacheControl = Request.Query.ContainsKey("v") ? "public, max-age=31536000, immutable" : "public, max-age=300";
            Response.Headers.XContentTypeOptions = "nosniff";
            return File(foto.Contenido, foto.TipoMime);
        }
        catch (ReglaNegocioException ex)
        {
            return NotFound(new ErrorNegocioDto(ex.Codigo, ex.Message));
        }
    }
}
