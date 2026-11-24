using Breaku.Application.Notificaciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Breaku.Presentation.Controllers;

/// <summary>
/// HU32: la pestaña «Alertas» del panel. Los estudiantes con alertas activas (intentos bloqueados por seguridad o reportes
/// acumulados), para revisarlos y decidir. El sistema nunca sanciona solo.
/// </summary>
[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("api/admin/alertas")]
[Tags("Administración")]
[Produces("application/json")]
public sealed class AdminAlertasController : ControllerBase
{
    private readonly AlertasAdminService _service;

    public AdminAlertasController(AlertasAdminService service) => _service = service;

    /// <summary>Estudiantes con alertas activas, del más reciente al más antiguo, con cada alerta y su texto.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AlertaEstudianteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await _service.ListarAsync(ct));

    /// <summary>
    /// Descarta las alertas de un estudiante sin sancionarlo: salen de la lista y de la campanita de todos los admins. Si
    /// vuelve a pasar, la alerta vuelve sola. Sancionarlo también las descarta.
    /// </summary>
    [HttpPatch("{usuarioId:int}/descartar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Descartar(int usuarioId, CancellationToken ct)
    {
        await _service.DescartarAsync(usuarioId, ct);
        return NoContent();
    }
}
