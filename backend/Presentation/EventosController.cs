using System.Security.Claims;
using Breaku.Application.DTOs;
using Breaku.Application.Eventos;
using Breaku.Application.Horarios;
using Breaku.Domain;
using Breaku.Domain.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/eventos")]
[Tags("Eventos")]
[Produces("application/json")]
public sealed class EventosController : ControllerBase
{
    private const long LimitePeticionFoto = ReglasEvento.BytesMaximosImagen + 512 * 1024;
    private readonly EventoService _service;
    private readonly EventosEnPuenteService _eventosEnPuente;

    public EventosController(EventoService service, EventosEnPuenteService eventosEnPuente)
    {
        _service = service;
        _eventosEnPuente = eventosEnPuente;
    }

    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    [HttpGet]
    [ProducesResponseType(typeof(List<EventoPublicoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Listar([FromQuery] string? rango, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.ListarPublicadosAsync(rango, ct));
        }
        catch (ReglaNegocioException ex)
        {
            return ResponderRegla(ex);
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventoPublicoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Detalle(int id, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.ObtenerPublicadoAsync(id, ct));
        }
        catch (ReglaNegocioException ex)
        {
            return ResponderRegla(ex);
        }
    }

    [HttpGet("lugares")]
    [ProducesResponseType(typeof(List<EventoLugarDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Lugares(CancellationToken ct)
        => Ok(await _service.ListarLugaresAsync(ct));

    [HttpGet("mios")]
    [ProducesResponseType(typeof(List<EventoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Mios(CancellationToken ct)
        => Ok(await _service.ListarMiosAsync(UsuarioId, ct));

    [HttpGet("en-puente")]
    [ProducesResponseType(typeof(List<EventoEnPuenteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> EnPuente(CancellationToken ct)
        => Ok(await _eventosEnPuente.ListarAsync(UsuarioId, ct));

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimitePeticionFoto)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimitePeticionFoto)]
    [ProducesResponseType(typeof(EventoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> Crear([FromForm] EventoFormularioForm form, CancellationToken ct)
    {
        try
        {
            var imagen = await LeerImagenAsync(form.Foto, ct);
            var creado = await _service.CrearAsync(
                UsuarioId, User.IsInRole("ADMIN"), form.ARequest(), imagen, ct);
            return StatusCode(StatusCodes.Status201Created, creado);
        }
        catch (ReglaNegocioException ex)
        {
            return ResponderRegla(ex);
        }
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimitePeticionFoto)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimitePeticionFoto)]
    [ProducesResponseType(typeof(EventoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> Editar(int id, [FromForm] EventoFormularioForm form, CancellationToken ct)
    {
        try
        {
            var imagen = await LeerImagenAsync(form.Foto, ct);
            return Ok(await _service.ActualizarAsync(id, UsuarioId, form.ARequest(), imagen, ct));
        }
        catch (ReglaNegocioException ex)
        {
            return ResponderRegla(ex);
        }
    }

    [HttpPatch("{id:int}/cancelar")]
    [ProducesResponseType(typeof(EventoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancelar(int id, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.CancelarAsync(id, UsuarioId, ct));
        }
        catch (ReglaNegocioException ex)
        {
            return ResponderRegla(ex);
        }
    }

    [HttpGet("{id:int}/foto")]
    [Produces("image/jpeg", "image/png", "application/json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Foto(int id, CancellationToken ct)
    {
        try
        {
            var foto = await _service.ObtenerImagenAsync(id, UsuarioId, User.IsInRole("ADMIN"), ct);
            Response.Headers.CacheControl = Request.Query.ContainsKey("v")
                ? "private, max-age=31536000, immutable"
                : "private, max-age=60";
            Response.Headers.XContentTypeOptions = "nosniff";
            return File(foto.Contenido, foto.TipoMime);
        }
        catch (ReglaNegocioException ex)
        {
            return ResponderRegla(ex);
        }
    }

    private static async Task<byte[]?> LeerImagenAsync(IFormFile? foto, CancellationToken ct)
    {
        if (foto is null) return null;
        if (foto.Length == 0) throw new ReglaNegocioException("VALIDACION", "La foto seleccionada está vacía.");
        if (foto.Length > ReglasEvento.BytesMaximosImagen)
            throw new ReglaNegocioException("VALIDACION", "La foto no puede superar los 2 MB.");

        await using var memoria = new MemoryStream((int)foto.Length);
        await foto.CopyToAsync(memoria, ct);
        return memoria.ToArray();
    }

    private IActionResult ResponderRegla(ReglaNegocioException ex)
    {
        var error = new ErrorNegocioDto(ex.Codigo, ex.Message);
        return ex.Codigo switch
        {
            "NO_ENCONTRADO" => NotFound(error),
            "LIMITE_DIARIO" or "EVENTO_INICIADO" or "EVENTO_NO_EDITABLE" or "YA_CANCELADO" or "YA_REVISADO"
                or ReglasEvento.CodigoEventoDuplicado => Conflict(error),
            _ => BadRequest(error)
        };
    }
}

public sealed class EventoFormularioForm
{
    public string? Titulo { get; set; }
    public string? Descripcion { get; set; }
    public DateTimeOffset? Inicio { get; set; }
    public DateTimeOffset? Fin { get; set; }
    public int? LugarId { get; set; }
    public string? LugarTexto { get; set; }
    public string? Asistencia { get; set; }
    public int? VisitaMin { get; set; }
    public int? VisitaMax { get; set; }
    /// <summary>FORMULARIO (por defecto) o FLYER. Solo se usa al crear; al editar se ignora.</summary>
    public string? Origen { get; set; }
    public IFormFile? Foto { get; set; }
    /// <summary>Solo al editar: true borra la foto actual. No se puede enviar junto con una Foto nueva.</summary>
    public bool QuitarFoto { get; set; }

    public EventoFormularioRequest ARequest()
        => new(Titulo, Descripcion, Inicio, Fin, LugarId, LugarTexto, Asistencia, VisitaMin, VisitaMax, Origen, QuitarFoto);
}
