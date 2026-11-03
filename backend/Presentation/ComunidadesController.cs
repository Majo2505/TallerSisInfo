using System.Security.Claims;
using Breaku.Application.Comunidades;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

/// <summary>
/// Comunidades efímeras. HU13: al crear una actividad (<c>POST /api/actividades</c>, HU10) se invita automáticamente a los
/// compañeros libres a esa hora y se les avisa; aquí se puede volver a buscar. Descubrir (adición a HU11) lista las
/// comunidades activas de hoy.
/// </summary>
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

    // El usuario sale SIEMPRE del token: nadie invita a la actividad de otro.
    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    /// <summary>Comunidades activas de hoy (Comunidades → Descubrir).</summary>
    /// <remarks>
    /// Las que todavía no terminaron y son de hoy (hora de Bolivia), de la más próxima a la más lejana, como máximo 50.
    /// Siempre aparecen las que creaste y aquellas a las que te invitaron. Las de tus compañeros, solo si elegiste campus y
    /// aceptaste aparecer en el matching (HU12): es el mismo consentimiento recíproco que usan las invitaciones de HU13
    /// (<c>SIN_ZONA</c> o <c>NO_VISIBLE</c> explican por qué no ves las de los demás). De quien la creó solo se muestra el
    /// nombre y la inicial del apellido. Es solo lectura: unirse o confirmar asistencia es de HU14.
    /// </remarks>
    /// <param name="ct">Cancelación de la petición.</param>
    /// <response code="200">Las comunidades y el estado (<c>OK</c>, <c>SIN_RESULTADOS</c>, <c>SIN_ZONA</c> o <c>NO_VISIBLE</c>).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
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

    /// <summary>Vuelve a buscar compañeros libres para tu actividad e invita a los que coincidan.</summary>
    /// <remarks>
    /// La primera búsqueda es automática al crear la actividad. Este endpoint sirve para intentarlo de nuevo: después de
    /// aceptar el matching (<c>NO_VISIBLE</c>), de elegir campus (<c>SIN_ZONA</c>) o si antes no había nadie libre
    /// (<c>SIN_COINCIDENCIAS</c>). Mismas reglas: compañeros de tu sede que aceptaron el matching (HU12), con un puente hoy
    /// que comparta 30 minutos o más con la actividad (o la actividad entera, si dura menos) y que no estén en otra
    /// comunidad a esa hora. Se invita solo a los lugares que quedan y nunca dos veces a la misma persona.
    /// </remarks>
    /// <param name="id">Id de tu actividad (el <c>id</c> que devolvió <c>POST /api/actividades</c>).</param>
    /// <param name="ct">Cancelación de la petición.</param>
    /// <response code="200">Resultado de la búsqueda (<c>estado</c> dice qué pasó).</response>
    /// <response code="400">La actividad ya terminó (<c>VALIDACION</c>).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="404">La actividad no existe o no es tuya (<c>NO_ENCONTRADO</c>).</response>
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
