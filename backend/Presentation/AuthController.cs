using Breaku.Application.Common.Interfaces;
using Breaku.Application.DTOs;
using Breaku.Application.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
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

    [HttpPost("google")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorMensajeDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorMensajeDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorMensajeDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Google([FromBody] GoogleLoginRequestDto request, CancellationToken ct)
    {
        try
        {
            return Ok(await _authService.LoginConGoogleAsync(request, ct));
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
