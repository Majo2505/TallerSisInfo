using System.Security.Claims;
using Breaku.Application.Actividades;
using Breaku.Application.DTOs;
using Breaku.Application.Horarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Api.Controllers;

/// <summary>
/// Extracción estructurada de actividades por tool calling (HU11). Convierte lo que el estudiante escribe en sus
/// palabras en datos consistentes (actividad, hora, duración y participantes). La creación de la actividad con
/// estos datos es de HU10.
/// </summary>
[ApiController]
[Authorize]
[Route("api/actividades")]
[Tags("Actividades")]
[Produces("application/json")]
public class ActividadesController : ControllerBase
{
    private readonly ExtraccionActividadService _service;
    public ActividadesController(ExtraccionActividadService service) => _service = service;

    // El usuario sale SIEMPRE del token (claim sub / NameIdentifier), nunca del body ni de la URL.
    private int UsuarioId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException());

    /// <summary>Extrae los datos de una actividad descrita en lenguaje natural.</summary>
    /// <remarks>
    /// El modelo recibe una sola función, <c>registrar_actividad</c>, y está obligado a llamarla: sus argumentos son el JSON
    /// con la actividad. Qué datos son válidos lo decide el servidor, no el modelo: hora de hoy entre 07:15 y 21:00 (en hora
    /// de Bolivia), duración de 10 a 240 minutos que no pase de las 21:00, y de 2 a 30 personas contando a quien la crea.
    /// Un dato que falta o no es válido va en <c>faltantes</c> y <c>mensaje</c> trae la pregunta por el primero.
    /// Si no dice la hora, la actividad empieza ahora y <c>supuestos</c> lo indica.
    ///
    /// Para responder la pregunta se vuelve a llamar con <c>texto</c> = la respuesta (por ejemplo «una hora») y
    /// <c>anterior</c> = el campo <c>actividad</c> de la respuesta anterior: no hace falta reescribir toda la descripción.
    ///
    /// El texto pasa antes por los guardrails de HU17: si intenta cambiar las instrucciones del asistente, pide datos internos
    /// o algo fuera del reglamento universitario, se rechaza con <c>400</c> y <c>SOLICITUD_NO_PERMITIDA</c> sin llamar al modelo.
    /// Toda extracción queda en el registro de auditoría con tipo <c>EXTRACCION_COMUNIDAD</c>.
    ///
    /// Ejemplo de petición:
    /// <code>
    /// { "texto": "Quiero jugar cartas por una hora", "anterior": null }
    /// </code>
    /// </remarks>
    /// <param name="req">Texto del estudiante y, si responde a una pregunta, lo ya entendido.</param>
    /// <param name="ct">Cancelación de la petición.</param>
    /// <response code="200">Datos extraídos: <c>COMPLETO</c>, <c>INCOMPLETO</c> (con la pregunta en <c>mensaje</c>) o <c>NO_ENTENDIDO</c>.</response>
    /// <response code="400">Falta el texto o supera los 200 caracteres (<c>codigo</c> = <c>VALIDACION</c>), o no se puede enviar al asistente (<c>codigo</c> = <c>SOLICITUD_NO_PERMITIDA</c>).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    /// <response code="503">El modelo no respondió (<c>codigo</c> = <c>ASISTENTE_NO_DISPONIBLE</c>). La extracción queda registrada como <c>ERROR</c>.</response>
    [HttpPost("extraer")]
    [ProducesResponseType(typeof(RespuestaExtraccionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> Extraer(SolicitudExtraccionDto req, CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.ExtraerAsync(UsuarioId, req, ct)));

    /// <summary>Revisa los datos de una actividad que el estudiante corrigió a mano, sin llamar a la IA.</summary>
    /// <remarks>
    /// Sirve cuando la IA entendió mal y el estudiante edita las casillas del resumen. Se aplican las mismas reglas que en
    /// <c>POST /api/actividades/extraer</c> y la respuesta tiene el mismo formato: con <c>COMPLETO</c>, su <c>consultaId</c>
    /// es el que se manda a <c>POST /api/actividades</c> para crear la actividad (HU10). Si un dato no es válido responde
    /// <c>INCOMPLETO</c> con la pregunta en <c>mensaje</c>. Queda en la auditoría con modelo <c>manual</c>.
    ///
    /// Para corregir <b>en palabras</b> («mejor a las 5») no hace falta este endpoint: se usa <c>POST /api/actividades/extraer</c>
    /// con <c>texto</c> = la corrección y <c>anterior</c> = lo ya entendido.
    ///
    /// Ejemplo de petición:
    /// <code>
    /// {
    ///   "texto": "Quiero jugar cartas por una hora",
    ///   "actividad": { "actividad": "jugar cartas", "horaInicio": "17:00", "duracionMin": 90, "maxParticipantes": 6 }
    /// }
    /// </code>
    /// </remarks>
    /// <param name="req">La descripción original y los datos corregidos.</param>
    /// <param name="ct">Cancelación de la petición.</param>
    /// <response code="200">Datos revisados: <c>COMPLETO</c> o <c>INCOMPLETO</c> (con la pregunta en <c>mensaje</c>).</response>
    /// <response code="400">Falta la descripción o la actividad (<c>codigo</c> = <c>VALIDACION</c>), o no pasa los guardrails (<c>codigo</c> = <c>SOLICITUD_NO_PERMITIDA</c>).</response>
    /// <response code="401">Falta el token, es inválido o está vencido.</response>
    [HttpPost("revisar")]
    [ProducesResponseType(typeof(RespuestaExtraccionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorNegocioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> Revisar(RevisionActividadDto req, CancellationToken ct)
        => Ejecutar(async () => Ok(await _service.RevisarAsync(UsuarioId, req, ct)));

    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> accion)
    {
        try
        {
            return await accion();
        }
        catch (ReglaNegocioException ex)
        {
            var body = new ErrorNegocioDto(ex.Codigo, ex.Message);
            return ex.Codigo == "ASISTENTE_NO_DISPONIBLE"
                ? StatusCode(StatusCodes.Status503ServiceUnavailable, body)
                : BadRequest(body);
        }
    }
}
