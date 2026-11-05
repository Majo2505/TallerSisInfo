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
    private readonly AsistenciaService _asistencia;

    public ComunidadesController(ComunidadService service, DescubrirComunidadesService descubrir, AsistenciaService asistencia)
    {
        _service = service;
        _descubrir = descubrir;
        _asistencia = asistencia;
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

    [HttpPut("{id:int}/asistencia")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AsistenciaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResponderAsistencia(int id, [FromBody] ResponderAsistenciaDto body, CancellationToken ct)
    {
        try
        {
            return Ok(await _asistencia.ResponderAsync(UsuarioId, id, body.Asisto, ct));
        }
        catch (ReglaNegocioException ex)
        {
            return Responder(ex);
        }
    }

    [HttpGet("{id:int}/participantes")]
    [ProducesResponseType(typeof(ParticipantesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Participantes(int id, CancellationToken ct)
    {
        try
        {
            return Ok(await _asistencia.ListarParticipantesAsync(UsuarioId, id, ct));
        }
        catch (ReglaNegocioException ex)
        {
            return Responder(ex);
        }
    }

    private IActionResult Responder(ReglaNegocioException ex)
    {
        var body = new ErrorNegocioDto(ex.Codigo, ex.Message);
        return ex.Codigo switch
        {
            "NO_ENCONTRADO" => NotFound(body),
            "COMPLETA" or "CRUCE" => Conflict(body),
            _ => BadRequest(body)
        };
    }
}
