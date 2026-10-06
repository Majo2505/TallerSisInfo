using System.Security.Claims;
using Breaku.Application.Horarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/horario")]
public class HorarioController : ControllerBase
{
    private readonly HorarioService _service;
    public HorarioController(HorarioService service) => _service = service;

    // El usuario sale SIEMPRE del token (claim sub / NameIdentifier).
    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    [HttpGet]
    public async Task<ActionResult<List<HorarioDto>>> Listar(CancellationToken ct)
        => Ok(await _service.ListarAsync(UsuarioId, ct));

    [HttpPost]
    public Task<IActionResult> Crear(GuardarHorarioRequest req, CancellationToken ct)
        => Ejecutar(async () => CreatedAtAction(nameof(Listar), await _service.CrearAsync(UsuarioId, req, ct)));

    [HttpPut("{id:int}")]
    public Task<IActionResult> Editar(int id, GuardarHorarioRequest req, CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.EditarAsync(UsuarioId, id, req, ct)));

    [HttpDelete("{id:int}")]
    public Task<IActionResult> Eliminar(int id, CancellationToken ct)
        => Ejecutar(async () => { await _service.EliminarAsync(UsuarioId, id, ct); return NoContent(); });

    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> accion)
    {
        try { return await accion(); }
        catch (ReglaNegocioException ex)
        {
            var body = new { codigo = ex.Codigo, mensaje = ex.Message };
            return ex.Codigo switch
            {
                "SOLAPAMIENTO" => Conflict(body),
                "NO_ENCONTRADO" => NotFound(body),
                _ => BadRequest(body)
            };
        }
    }
}
