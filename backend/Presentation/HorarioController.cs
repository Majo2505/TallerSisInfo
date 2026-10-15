using System.Security.Claims;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

/// <summary>
/// Horario académico del estudiante (HU2): alta, edición, consulta y eliminación de bloques de clase.
/// Todos los endpoints requieren JWT y trabajan solo con los bloques del usuario del token.
/// </summary>
[ApiController]
[Authorize]
[Route("api/horario")]
[Tags("Horario")]
[Produces("application/json")]
public class HorarioController : ControllerBase
{
    private readonly HorarioService _service;
    public HorarioController(HorarioService service) => _service = service;

    // El usuario sale SIEMPRE del token (claim sub / NameIdentifier).
    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    /// <summary>Lista los bloques de clase del usuario.</summary>
    /// <remarks>Ordenados por día de la semana y luego por hora de inicio. Sin bloques devuelve una lista vacía (<c>[]</c>). No hay paginación.</remarks>
    /// <response code="200">Lista de bloques del usuario.</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<HorarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<HorarioDto>>> Listar(CancellationToken ct)
        => Ok(await _service.ListarAsync(UsuarioId, ct));

    /// <summary>Crea un bloque de clase.</summary>
    /// <remarks>
    /// Reglas: materia obligatoria (máx. 100 caracteres), <c>diaSemana</c> de 1 a 6, horas en formato <c>HH:mm</c> o <c>HH:mm:ss</c>
    /// y <c>horaFin</c> posterior a <c>horaInicio</c>. No puede solaparse con otro bloque del mismo día; dos bloques contiguos
    /// (uno termina justo cuando empieza el otro) <b>no</b> se consideran solapados.
    ///
    /// Efecto secundario: al guardar se recalculan automáticamente los puentes del usuario (ver <c>GET /api/puentes</c>).
    ///
    /// Ejemplo de petición:
    /// <code>
    /// {
    ///   "materia": "Cálculo I",
    ///   "diaSemana": 1,
    ///   "horaInicio": "08:00",
    ///   "horaFin": "09:30",
    ///   "aula": "A-101"
    /// }
    /// </code>
    /// </remarks>
    /// <param name="req">Datos del bloque.</param>
    /// <response code="201">Bloque creado. Devuelve el bloque con su <c>id</c> y las horas en <c>HH:mm:ss</c>.</response>
    /// <response code="400">Datos inválidos (<c>codigo</c> = <c>VALIDACION</c>), o cuerpo mal formado / con campos faltantes (en ese caso la respuesta es un <c>ProblemDetails</c> estándar de ASP.NET).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="409">Se solapa con otro bloque del mismo día (<c>codigo</c> = <c>SOLAPAMIENTO</c>); el mensaje indica con cuál.</response>
    [HttpPost]
    [ProducesResponseType(typeof(HorarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Crear(GuardarHorarioRequest req, CancellationToken ct)
        => Ejecutar(async () => CreatedAtAction(nameof(Listar), await _service.CrearAsync(UsuarioId, req, ct)));

    /// <summary>Reemplaza por completo un bloque de clase existente.</summary>
    /// <remarks>
    /// Es un <c>PUT</c> de reemplazo completo: se envían todos los campos, igual que en el alta. Aplican las mismas reglas
    /// que en <c>POST</c>; el bloque que se edita no se compara consigo mismo al buscar solapamientos.
    /// Si el bloque pertenece a otro usuario se responde igual que si no existiera (404).
    ///
    /// Efecto secundario: se recalculan automáticamente los puentes del usuario.
    /// </remarks>
    /// <param name="id" example="12">Identificador del bloque.</param>
    /// <param name="req">Nuevos datos del bloque.</param>
    /// <response code="200">Bloque actualizado.</response>
    /// <response code="400">Datos inválidos (<c>codigo</c> = <c>VALIDACION</c>).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="404">El bloque no existe o no pertenece al usuario (<c>codigo</c> = <c>NO_ENCONTRADO</c>).</response>
    /// <response code="409">Se solapa con otro bloque del mismo día (<c>codigo</c> = <c>SOLAPAMIENTO</c>).</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(HorarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Editar(int id, GuardarHorarioRequest req, CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.EditarAsync(UsuarioId, id, req, ct)));

    /// <summary>Elimina un bloque de clase.</summary>
    /// <remarks>
    /// Si el bloque pertenece a otro usuario se responde igual que si no existiera (404).
    /// Efecto secundario: se recalculan automáticamente los puentes del usuario.
    /// </remarks>
    /// <param name="id" example="12">Identificador del bloque.</param>
    /// <response code="204">Bloque eliminado (sin cuerpo).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="404">El bloque no existe o no pertenece al usuario (<c>codigo</c> = <c>NO_ENCONTRADO</c>).</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Eliminar(int id, CancellationToken ct)
        => Ejecutar(async () => { await _service.EliminarAsync(UsuarioId, id, ct); return NoContent(); });

    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> accion)
    {
        try { return await accion(); }
        catch (ReglaNegocioException ex)
        {
            var body = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo switch
            {
                "SOLAPAMIENTO" => Conflict(body),
                "NO_ENCONTRADO" => NotFound(body),
                _ => BadRequest(body)
            };
        }
    }
}
