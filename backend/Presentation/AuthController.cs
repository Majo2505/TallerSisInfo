using Breaku.Application.Common.Interfaces;
using Breaku.Application.DTOs;
using Breaku.Application.DTOs.Auth;
using Breaku.Domain.Rules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace Breaku.Presentation.Controllers;

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

    /// <summary>Entra con Google: el frontend manda el token que le dio Google Identity Services.</summary>
    /// <remarks>
    /// Solo cuentas @ucb.edu.bo con el correo verificado. Si ya hay una cuenta con ese correo, se enlaza; si no, se crea
    /// como estudiante (sin contraseña: la puede crear en Perfil → Seguridad). El administrador entra solo con contraseña.
    /// </remarks>
    /// <response code="200">Sesión iniciada (mismo cuerpo que el login).</response>
    /// <response code="400">Cuenta no institucional, correo sin verificar, administrador o cuenta desactivada.</response>
    /// <response code="401">El token de Google no es válido o venció.</response>
    /// <response code="503">Falta GOOGLE_CLIENT_ID en el servidor.</response>
    [HttpPost("google")]
    [EnableRateLimiting(ReglasCuenta.PoliticaLogin)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorMensajeDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorMensajeDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorCuentaBloqueadaDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorMensajeDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Google([FromBody] LoginGoogleRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _authService.LoginConGoogleAsync(request.Credencial, ct));
        }
        catch (CuentaBloqueadaException ex)
        {
            return CuentaBloqueada(ex);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorMensajeDto(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ErrorMensajeDto(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorMensajeDto(ex.Message));
        }
    }

    [HttpPost("login")]
    [EnableRateLimiting(ReglasCuenta.PoliticaLogin)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorMensajeDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorMensajeDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorCuentaBloqueadaDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }
        catch (CuentaBloqueadaException ex)
        {
            return CuentaBloqueada(ex);
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

    /// <summary>HU28: 403 con el motivo y, si la hay, la fecha de fin de la suspensión (UTC).</summary>
    private ObjectResult CuentaBloqueada(CuentaBloqueadaException ex)
    {
        var codigo = ex.Bloqueo.Tipo switch
        {
            "SUSPENSION" => "CUENTA_SUSPENDIDA",
            "BAJA" => "CUENTA_DADA_DE_BAJA",
            _ => "CUENTA_DESACTIVADA"
        };
        return StatusCode(StatusCodes.Status403Forbidden, new ErrorCuentaBloqueadaDto(ex.Message, codigo, ex.Bloqueo.Motivo, ex.Bloqueo.Hasta));
    }
}
