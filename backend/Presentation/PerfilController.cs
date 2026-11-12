using System.Security.Claims;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Breaku.Application.Perfil;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Breaku.Domain.Rules;

namespace Breaku.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/perfil")]
[Tags("Perfil")]
[Produces("application/json")]
public class PerfilController : ControllerBase
{
    private readonly PerfilService _service;
    public PerfilController(PerfilService service) => _service = service;

    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    [HttpGet]
    [ProducesResponseType(typeof(PerfilDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Obtener(CancellationToken ct) => Ejecutar(async () => Ok(await _service.ObtenerAsync(UsuarioId, ct)));

    [HttpPut]
    [ProducesResponseType(typeof(PerfilDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Actualizar(ActualizarPerfilRequest req, CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.ActualizarAsync(UsuarioId, req, ct)));

    /// <summary>Cambia la contraseña (o la crea si la cuenta entra solo con Google).</summary>
    /// <response code="204">Contraseña guardada.</response>
    /// <response code="400">Falta la actual, es incorrecta (<c>CONTRASENA_INCORRECTA</c>) o la nueva no cumple las reglas (<c>VALIDACION</c>).</response>
    /// <response code="429">Demasiados intentos en un minuto.</response>
    [HttpPut("contrasena")]
    [EnableRateLimiting(ReglasCuenta.PoliticaContrasena)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status429TooManyRequests)]
    public Task<IActionResult> CambiarContrasena(CambiarContrasenaRequest req, CancellationToken ct)
        => Ejecutar(async () =>
        {
            await _service.CambiarContrasenaAsync(UsuarioId, req, ct);
            return NoContent();
        });

    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> accion)
    {
        try { return await accion(); }
        catch (ReglaNegocioException ex)
        {
            var body = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo == "NO_ENCONTRADO" ? NotFound(body) : BadRequest(body);
        }
    }
}
