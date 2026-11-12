using System.Security.Claims;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Breaku.Domain.Rules;

namespace Breaku.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/horario")]
[Tags("Horario")]
[Produces("application/json")]
public class FotoHorarioController : ControllerBase
{
    private const long LimiteCuerpo = FotoHorarioService.BytesMaximos + 1024 * 1024;

    private readonly FotoHorarioService _foto;
    private readonly HorarioService _horario;

    public FotoHorarioController(FotoHorarioService foto, HorarioService horario)
    {
        _foto = foto;
        _horario = horario;
    }

    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    [HttpPost("foto")]
    [EnableRateLimiting(ReglasRendimiento.PoliticaIa)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimiteCuerpo)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteCuerpo)]
    [ProducesResponseType(typeof(LecturaFotoHorarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> LeerFoto(IFormFile? foto, CancellationToken ct) => Ejecutar(async () =>
    {
        if (foto is null) throw new ReglaNegocioException("VALIDACION", "Adjunta la foto de tu horario en el campo «foto».");
        if (foto.Length > FotoHorarioService.BytesMaximos) throw new ReglaNegocioException("VALIDACION", "La foto no puede superar los 8 MB.");

        using var memoria = new MemoryStream();
        await foto.CopyToAsync(memoria, ct);
        return Ok(await _foto.LeerAsync(memoria.ToArray(), foto.ContentType, ct));
    });

    [HttpPost("lote")]
    [ProducesResponseType(typeof(ResultadoLoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> GuardarLote(GuardarLoteRequest req, CancellationToken ct)
        => Ejecutar(async () => Ok(await _horario.CrearLoteAsync(UsuarioId, req, ct)));

    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> accion)
    {
        try { return await accion(); }
        catch (ReglaNegocioException ex)
        {
            var body = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo == "LECTOR_NO_DISPONIBLE" ? StatusCode(StatusCodes.Status503ServiceUnavailable, body) : BadRequest(body);
        }
    }
}
