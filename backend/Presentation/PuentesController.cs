using System.Security.Claims;
using Breaku.Application.Puentes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

/// <summary>
/// Puentes detectados (HU3): huecos libres de 45 minutos o más entre dos clases del mismo día.
/// Solo consulta: los puentes se recalculan solos cada vez que se crea, edita o elimina un bloque del horario.
/// </summary>
[ApiController]
[Authorize]
[Route("api/puentes")]
[Tags("Puentes")]
[Produces("application/json")]
public class PuenteController : ControllerBase
{
    private readonly PuenteService _service;
    public PuenteController(PuenteService service) => _service = service;

    // El usuario sale SIEMPRE del token (claim sub / NameIdentifier), nunca del body ni de la URL.
    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    /// <summary>Puentes detectados del usuario, ordenados por día y hora. Sin puentes: [].</summary>
    /// <remarks>
    /// Un puente es el hueco entre el fin de una clase y el inicio de la siguiente, el mismo día, de <b>45 minutos o más</b>
    /// (45 exactos cuentan). El tramo antes de la primera clase y después de la última no es puente.
    /// No hay endpoint para recalcular: ocurre dentro de <c>POST</c>, <c>PUT</c> y <c>DELETE</c> de <c>/api/horario</c>.
    /// La duración no viene en la respuesta: se calcula como <c>horaFin − horaInicio</c>.
    /// </remarks>
    /// <response code="200">Lista de puentes del usuario (puede ser vacía).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<PuenteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<PuenteDto>>> Listar(CancellationToken ct)
        => Ok(await _service.ListarAsync(UsuarioId, ct));
}
