using System.Security.Claims;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Breaku.Application.Sanciones;
using Breaku.Domain.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

/// <summary>HU28: el administrador ve a los estudiantes y los sanciona (advertencia, suspensión o baja).</summary>
[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("api/admin/usuarios")]
[Tags("Administración · Usuarios y sanciones")]
[Produces("application/json")]
public class AdminUsuariosController : ControllerBase
{
    private readonly SancionService _service;
    private readonly ILogger<AdminUsuariosController> _logger;

    public AdminUsuariosController(SancionService service, ILogger<AdminUsuariosController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private int AdminId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    /// <summary>Lista de estudiantes con su estado y su cantidad de sanciones, con búsqueda por nombre o correo.</summary>
    /// <param name="buscar">Texto que debe contener el nombre o el correo.</param>
    /// <param name="limite">Cuántos devolver (100 por defecto, máximo 200).</param>
    /// <param name="ct">Cancelación de la petición.</param>
    [HttpGet]
    [ProducesResponseType(typeof(List<UsuarioListaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> Listar([FromQuery] string? buscar, [FromQuery] int? limite, CancellationToken ct)
        => Responder(async () => Ok(await _service.ListarAsync(buscar, limite, ct)));

    /// <summary>Detalle del estudiante con su historial de sanciones y su conteo de advertencias (las levantadas no cuentan).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UsuarioDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Obtener(int id, CancellationToken ct)
        => Responder(async () => Ok(await _service.ObtenerAsync(id, ct)));

    /// <summary>Aplica una sanción. El motivo es obligatorio; la suspensión sin <c>hasta</c> dura 7 días; la baja es definitiva y purga los datos del usuario.</summary>
    [HttpPost("{id:int}/sanciones")]
    [ProducesResponseType(typeof(SancionAplicadaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Sancionar(int id, [FromBody] SancionarUsuarioDto req, CancellationToken ct)
        => Responder(async () =>
        {
            var aplicada = await _service.AplicarAsync(AdminId, id, req, ct);
            return Created($"/api/admin/usuarios/{id}", aplicada);
        });

    /// <summary>Levanta una advertencia o una suspensión (queda quién y cuándo). La baja no se levanta.</summary>
    [HttpPost("{id:int}/sanciones/{sancionId:int}/levantar")]
    [ProducesResponseType(typeof(SancionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Levantar(int id, int sancionId, CancellationToken ct)
        => Responder(async () => Ok(await _service.LevantarAsync(AdminId, id, sancionId, ct)));

    /// <summary>
    /// Las reglas de negocio responden con su código (400 por defecto, 404 si no existe, 409 si el estado actual lo impide);
    /// un error inesperado se registra con el id de traza y responde un 500 controlado.
    /// </summary>
    private async Task<IActionResult> Responder(Func<Task<IActionResult>> accion)
    {
        try
        {
            return await accion();
        }
        catch (ReglaNegocioException ex)
        {
            var cuerpo = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo switch
            {
                ReglasSancion.ErrorNoEncontrado => NotFound(cuerpo),
                ReglasSancion.ErrorYaSuspendido or ReglasSancion.ErrorYaDeBaja or ReglasSancion.ErrorYaLevantada
                    or ReglasSancion.ErrorBajaNoSeLevanta => Conflict(cuerpo),
                _ => BadRequest(cuerpo)
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var traza = HttpContext.TraceIdentifier;
            _logger.LogError(ex, "Error inesperado en la gestión de usuarios y sanciones (traza {Traza})", traza);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorNegocioDto("ERROR_INESPERADO", $"No pudimos completar la operación. Código de seguimiento: {traza}."));
        }
    }
}
