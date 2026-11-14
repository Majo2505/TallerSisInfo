using System.Security.Claims;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Breaku.Application.Matching;
using Breaku.Domain.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

/// <summary>HU12: el test de afinidad de «Libres contigo» (unos 2 minutos).</summary>
[ApiController]
[Authorize]
[Route("api/afinidad")]
[Tags("Matching")]
[Produces("application/json")]
public class AfinidadController : ControllerBase
{
    private readonly AfinidadService _service;
    public AfinidadController(AfinidadService service) => _service = service;

    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    /// <summary>Las preguntas del test y, si ya lo hiciste, tus respuestas.</summary>
    [HttpGet("test")]
    [ProducesResponseType(typeof(TestAfinidadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Obtener(CancellationToken ct) => Ok(await _service.ObtenerAsync(UsuarioId, ct));

    /// <summary>Guarda (o reemplaza) tus respuestas.</summary>
    [HttpPut("test")]
    [ProducesResponseType(typeof(TestAfinidadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Guardar(GuardarTestRequest req, CancellationToken ct)
    {
        try { return Ok(await _service.GuardarAsync(UsuarioId, req, ct)); }
        catch (ReglaNegocioException ex) { return BadRequest(new ErrorNegocioDto(ex.Codigo, ex.Message)); }
    }
}

/// <summary>HU12: saludar a quien coincide contigo y responder saludos.</summary>
[ApiController]
[Authorize]
[Route("api/conexiones")]
[Tags("Matching")]
[Produces("application/json")]
public class ConexionesController : ControllerBase
{
    private readonly ConexionService _service;
    public ConexionesController(ConexionService service) => _service = service;

    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    /// <summary>Saludos recibidos (pendientes), enviados (pendientes) y conexiones aceptadas (con correo).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ConexionesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await _service.ListarAsync(UsuarioId, ct));

    /// <summary>Saluda a alguien que «Libres contigo» te muestra para ese puente.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ConexionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Saludar(SaludarRequest req, CancellationToken ct)
        => Ejecutar(async () =>
        {
            var creada = await _service.SaludarAsync(UsuarioId, req, ct);
            return StatusCode(StatusCodes.Status201Created, creada);
        });

    /// <summary>Acepta o rechaza un saludo recibido.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ConexionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Responder(int id, ResponderConexionRequest req, CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.ResponderAsync(UsuarioId, id, req, ct)));

    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> accion)
    {
        try { return await accion(); }
        catch (ReglaNegocioException ex)
        {
            var cuerpo = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo switch
            {
                "NO_ENCONTRADO" => NotFound(cuerpo),
                ReglasConexion.ErrorYaExiste or ReglasConexion.ErrorDemasiados or ReglasConexion.ErrorNoCoinciden
                    or ReglasConexion.ErrorYaRespondida => Conflict(cuerpo),
                _ => BadRequest(cuerpo),
            };
        }
    }
}
