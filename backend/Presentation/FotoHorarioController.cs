using System.Security.Claims;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

/// <summary>
/// Registro del horario con una foto (HU2): un modelo de visión lee las clases, el estudiante las revisa
/// y confirma, y se guardan de una vez con origen <c>FOTO</c>.
/// </summary>
[ApiController]
[Authorize]
[Route("api/horario")]
[Tags("Horario")]
[Produces("application/json")]
public class FotoHorarioController : ControllerBase
{
    // Margen sobre los 8 MB de la foto para el resto del formulario multipart.
    private const long LimiteCuerpo = FotoHorarioService.BytesMaximos + 1024 * 1024;

    private readonly FotoHorarioService _foto;
    private readonly HorarioService _horario;

    public FotoHorarioController(FotoHorarioService foto, HorarioService horario)
    {
        _foto = foto;
        _horario = horario;
    }

    // El usuario sale SIEMPRE del token (claim sub / NameIdentifier), nunca del body ni de la URL.
    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    /// <summary>Lee las clases de la foto de un horario. No guarda nada.</summary>
    /// <remarks>
    /// Se envía como <c>multipart/form-data</c> con el campo <c>foto</c> (JPG, PNG o WebP, hasta 8 MB).
    /// Las clases que devuelve ya cumplen las reglas de un alta manual (día de 1 a 6, horas válidas, materia
    /// de hasta 100 caracteres); las que no las cumplen se cuentan en <c>descartadas</c>. Para guardarlas,
    /// el estudiante las revisa y las envía a <c>POST /api/horario/lote</c>.
    /// </remarks>
    /// <param name="foto">Imagen del horario.</param>
    /// <param name="ct">Cancelación de la petición.</param>
    /// <response code="200">Clases leídas (puede ser una lista vacía si la imagen no es un horario).</response>
    /// <response code="400">Falta la foto, está vacía, supera los 8 MB o no es JPG, PNG ni WebP (<c>codigo</c> = <c>VALIDACION</c>).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="503">El modelo de imagen no está configurado o no respondió (<c>codigo</c> = <c>LECTOR_NO_DISPONIBLE</c>).</response>
    [HttpPost("foto")]
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

    /// <summary>Guarda varios bloques de una vez (los confirmados desde la foto), con origen <c>FOTO</c>.</summary>
    /// <remarks>
    /// Cada bloque se valida igual que en <c>POST /api/horario</c>. Los que no pasan o se solapan con el horario
    /// (o con otro bloque del mismo lote) vuelven en <c>rechazados</c> con el motivo; el resto se guarda.
    /// Los puentes se recalculan una sola vez al final. Hasta 40 bloques por lote.
    /// </remarks>
    /// <param name="req">Bloques confirmados.</param>
    /// <param name="ct">Cancelación de la petición.</param>
    /// <response code="200">Bloques guardados y rechazados.</response>
    /// <response code="400">El lote está vacío o tiene más de 40 bloques (<c>codigo</c> = <c>VALIDACION</c>).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
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
