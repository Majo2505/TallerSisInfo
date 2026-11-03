using System.Security.Claims;
using Breaku.Application.Comunidades;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

/// <summary>
/// Comunidades efímeras. HU13: creación automática de la comunidad cuando hay compañeros libres a la hora de la actividad,
/// con invitación y aviso para cada uno.
/// </summary>
[ApiController]
[Authorize]
[Route("api/comunidades")]
[Tags("Comunidades")]
[Produces("application/json")]
public class ComunidadesController : ControllerBase
{
    private readonly ComunidadService _service;
    public ComunidadesController(ComunidadService service) => _service = service;

    // El usuario sale SIEMPRE del token: nadie crea una comunidad a nombre de otro.
    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    /// <summary>Crea la comunidad automáticamente si hay compañeros libres a esa hora, y les avisa.</summary>
    /// <remarks>
    /// Parte de una actividad que <c>POST /api/actividades/extraer</c> (HU11) dejó <c>COMPLETO</c>: se manda su <c>consultaId</c>
    /// y su <c>actividad</c> tal cual. El servidor vuelve a validar los datos con las reglas de HU11 (hora de hoy entre 07:15 y
    /// 21:00 que no haya pasado, de 10 a 240 minutos, de 2 a 30 personas).
    ///
    /// <b>Coincidencias:</b> se cruzan la hora de la actividad con los puentes de hoy de los compañeros de tu sede que aceptaron
    /// aparecer en el matching (HU12). Cuenta quien comparte <b>30 minutos o más</b> (o la actividad entera, si dura menos) y
    /// no está ya en otra comunidad a esa hora.
    ///
    /// <b>Creación automática:</b> solo si hay al menos una coincidencia. Se invita a los más compatibles, <b>uno por plaza</b>
    /// (<c>maxParticipantes - 1</c>), y cada uno recibe un aviso <c>INVITACION_COMUNIDAD</c> en <c>GET /api/notificaciones</c>
    /// (salvo quien apagó los avisos). Comunidad, invitaciones y avisos se guardan en una sola transacción.
    ///
    /// Sin coincidencias (<c>SIN_COINCIDENCIAS</c>), sin campus (<c>SIN_ZONA</c>) o sin aceptar el matching (<c>NO_VISIBLE</c>)
    /// responde 200 y <b>no crea nada</b>.
    ///
    /// Ejemplo de petición:
    /// <code>
    /// { "consultaId": 58, "texto": "Quiero jugar cartas a las 10:30 por una hora",
    ///   "actividad": { "actividad": "jugar cartas", "horaInicio": "10:30", "duracionMin": 60, "maxParticipantes": 4 } }
    /// </code>
    /// </remarks>
    /// <param name="req">La extracción de HU11 y, opcionalmente, el texto original.</param>
    /// <param name="ct">Cancelación de la petición.</param>
    /// <response code="201">Comunidad creada (<c>CREADA</c>), con los invitados.</response>
    /// <response code="200">No se creó: <c>SIN_COINCIDENCIAS</c>, <c>SIN_ZONA</c> o <c>NO_VISIBLE</c>, con el mensaje para mostrar.</response>
    /// <response code="400">Faltan datos o no son válidos (<c>VALIDACION</c>; <c>mensaje</c> dice cuál).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="404">La extracción no existe, no es tuya, no es de hoy o no estaba completa (<c>NO_ENCONTRADO</c>).</response>
    /// <response code="409">Esa extracción ya creó una comunidad (<c>YA_CREADA</c>) o ya estás en otra a esa hora (<c>EN_OTRA_COMUNIDAD</c>).</response>
    [HttpPost]
    [ProducesResponseType(typeof(ResultadoComunidadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResultadoComunidadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(CrearComunidadRequest req, CancellationToken ct)
    {
        try
        {
            var resultado = await _service.CrearAsync(UsuarioId, req, ct);
            return resultado.Estado == ComunidadService.EstadoCreada
                ? StatusCode(StatusCodes.Status201Created, resultado)
                : Ok(resultado);
        }
        catch (ReglaNegocioException ex)
        {
            var body = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo switch
            {
                "NO_ENCONTRADO" => NotFound(body),
                "YA_CREADA" or "EN_OTRA_COMUNIDAD" => Conflict(body),
                _ => BadRequest(body)
            };
        }
    }
}
