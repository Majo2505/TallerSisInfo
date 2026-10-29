using System.Security.Claims;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Breaku.Application.Perfil;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

/// <summary>
/// Gestión del perfil (HU21): el usuario autenticado ve sus datos y edita nombre, carrera y semestre.
/// El correo institucional y el rol no se pueden modificar.
/// </summary>
[ApiController]
[Authorize]
[Route("api/perfil")]
[Tags("Perfil")]
[Produces("application/json")]
public class PerfilController : ControllerBase
{
    private readonly PerfilService _service;
    public PerfilController(PerfilService service) => _service = service;

    // El usuario sale SIEMPRE del token: nadie ve ni edita el perfil de otro.
    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    /// <summary>Devuelve el perfil del usuario autenticado.</summary>
    /// <param name="ct">Cancelación de la petición.</param>
    /// <response code="200">Nombre, correo institucional, rol, carrera y semestre.</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="404">La cuenta no existe o está dada de baja (<c>codigo</c> = <c>NO_ENCONTRADO</c>).</response>
    [HttpGet]
    [ProducesResponseType(typeof(PerfilDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Obtener(CancellationToken ct) => Ejecutar(async () => Ok(await _service.ObtenerAsync(UsuarioId, ct)));

    /// <summary>Actualiza nombre, carrera y semestre.</summary>
    /// <remarks>
    /// El nombre es obligatorio (hasta 100 caracteres); la carrera (hasta 80) y el semestre (1 a 10) son opcionales.
    /// Si se envía <c>correo</c> o <c>rol</c> distintos a los actuales, se rechaza con <c>NO_EDITABLE</c>.
    /// El cambio queda en <c>historial_cambio</c> con los valores de antes y de después.
    ///
    /// Ejemplo de petición:
    /// <code>
    /// { "nombre": "Ana Pérez", "carrera": "Ingeniería de Sistemas", "semestre": 5 }
    /// </code>
    /// </remarks>
    /// <param name="req">Campos editables.</param>
    /// <param name="ct">Cancelación de la petición.</param>
    /// <response code="200">Perfil actualizado.</response>
    /// <response code="400">Nombre vacío o datos fuera de rango (<c>VALIDACION</c>), o intento de cambiar correo o rol (<c>NO_EDITABLE</c>).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="404">La cuenta no existe o está dada de baja (<c>codigo</c> = <c>NO_ENCONTRADO</c>).</response>
    [HttpPut]
    [ProducesResponseType(typeof(PerfilDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Actualizar(ActualizarPerfilRequest req, CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.ActualizarAsync(UsuarioId, req, ct)));

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
