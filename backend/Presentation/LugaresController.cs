using System.Security.Claims;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Breaku.Application.Lugares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Presentation.Controllers;

/// <summary>
/// Consulta de lugares del catálogo viables para un puente (HU6): filtrados por tiempo disponible, cercanía al campus
/// del estudiante y zona verificada. Solo se muestran lugares aprobados por el administrador.
/// </summary>
[ApiController]
[Authorize]
[Route("api/lugares")]
[Tags("Lugares")]
[Produces("application/json")]
public class LugaresController : ControllerBase
{
    private readonly BusquedaLugaresService _service;
    public LugaresController(BusquedaLugaresService service) => _service = service;

    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    /// <summary>Lista los lugares viables para un puente del estudiante.</summary>
    /// <remarks>
    /// Un lugar es viable si cumple las tres condiciones:
    /// <list type="bullet">
    /// <item>está en estado <c>ACTIVO</c> (verificado y aprobado por el administrador);</item>
    /// <item>queda dentro del radio de la zona (campus) del estudiante, definida en HU5;</item>
    /// <item>el traslado de ida y vuelta (caminando, unos 80 m por minuto) más la actividad no supera la duración del puente.</item>
    /// </list>
    /// Los lugares salen ordenados del más cercano al más lejano. Si el estudiante todavía no eligió su campus se responde
    /// <c>200</c> con <c>estado</c> = <c>SIN_ZONA</c>; si ningún lugar cumple, <c>estado</c> = <c>SIN_RESULTADOS</c> y el mensaje
    /// «No encontramos opciones cercanas para este tiempo disponible.».
    /// </remarks>
    /// <param name="puenteId">Id de un puente del propio estudiante (<c>GET /api/puentes</c>).</param>
    /// <param name="ct">Cancelación de la petición.</param>
    /// <response code="200">Resultado de la búsqueda: <c>OK</c>, <c>SIN_RESULTADOS</c> o <c>SIN_ZONA</c>.</response>
    /// <response code="400"><c>puenteId</c> inválido (<c>codigo</c> = <c>VALIDACION</c>).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="404">El puente no existe o no pertenece al usuario (<c>codigo</c> = <c>NO_ENCONTRADO</c>).</response>
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
}
