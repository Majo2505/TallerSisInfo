using Breaku.Application.Common.Interfaces;
using Breaku.Application.DTOs;
using Breaku.Application.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
namespace Breaku.Presentation.Controllers;

/// <summary>Registro e inicio de sesión (HU1). Ambos devuelven un token JWT para usar en el resto de la API.</summary>
[ApiController]
[Route("api/auth")]
[Tags("Auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Crea una cuenta de estudiante y la deja con la sesión iniciada.</summary>
    /// <remarks>
    /// Solo se aceptan correos institucionales <c>usuario@ucb.edu.bo</c>, y el correo no puede estar ya registrado.
    /// La cuenta se crea con rol <c>ESTUDIANTE</c> y la contraseña se guarda con hash BCrypt.
    /// Como la respuesta ya incluye el token, no hace falta llamar a <c>login</c> después.
    ///
    /// Ejemplo de petición:
    /// <code>
    /// {
    ///   "nombre": "Ana Pérez",
    ///   "email": "ana.perez@ucb.edu.bo",
    ///   "password": "MiClaveSegura-2026"
    /// }
    /// </code>
    /// </remarks>
    /// <param name="request">Nombre, correo institucional y contraseña.</param>
    /// <response code="200">Cuenta creada. Devuelve el token JWT y los datos del usuario.</response>
    /// <response code="400">El correo no es <c>@ucb.edu.bo</c> o ya está registrado. El texto viene en <c>message</c>.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorMensajeDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(result);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new ErrorMensajeDto(ex.Message));
        }
    }

    /// <summary>Inicia sesión con correo institucional y contraseña.</summary>
    /// <remarks>
    /// Orden de validación: primero el dominio del correo (<c>@ucb.edu.bo</c>) y luego las credenciales.
    /// El token devuelto vence a las 24 horas; envíalo en el header <c>Authorization: Bearer {token}</c>.
    ///
    /// Ejemplo de petición (usuario de prueba de desarrollo, ver README):
    /// <code>
    /// {
    ///   "email": "dev.estudiante@ucb.edu.bo",
    ///   "password": "Breaku-Dev-2026!"
    /// }
    /// </code>
    /// </remarks>
    /// <param name="request">Correo institucional y contraseña.</param>
    /// <response code="200">Credenciales correctas. Devuelve el token JWT y los datos del usuario.</response>
    /// <response code="400">El correo no es <c>@ucb.edu.bo</c>. El texto viene en <c>message</c>.</response>
    /// <response code="401">Correo no registrado o contraseña incorrecta (<c>message</c>: «Credenciales inválidas.»; el mismo texto en ambos casos).</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorMensajeDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorMensajeDto), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorMensajeDto(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ErrorMensajeDto(ex.Message));
        }
    }
}
