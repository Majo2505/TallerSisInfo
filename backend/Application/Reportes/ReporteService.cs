using Breaku.Application.Horarios;
using Breaku.Domain.Rules;

namespace Breaku.Application.Reportes;

/// <summary>
/// Reportes de los estudiantes: «algo no funciona», sugerencias e información incorrecta de un lugar. Quedan PENDIENTE y
/// los revisa un administrador en el panel. Para que nadie llene la bandeja del admin: un mismo lugar no se reporta dos
/// veces mientras siga pendiente, y cada estudiante tiene como mucho 3 reportes pendientes a la vez.
/// </summary>
public class ReporteService
{
    private readonly IReporteRepository _repo;
    public ReporteService(IReporteRepository repo) => _repo = repo;

    public Task<List<LugarReportableDto>> ListarLugaresAsync(CancellationToken ct = default) => _repo.ListarLugaresAsync(ct);

    public async Task<ReporteCreadoDto> CrearAsync(int usuarioId, NuevoReporteDto req, CancellationToken ct = default)
    {
        var error = ReglasReporte.ValidarNuevo(req.Motivo, req.LugarId, req.Descripcion);
        if (error is not null) throw new ReglaNegocioException("VALIDACION", error);

        var motivo = ReglasReporte.LeerMotivo(req.Motivo)!;
        var descripcion = ReglasSeguridadIa.Sanear(req.Descripcion!);   // sin caracteres de control (el admin lo lee en el panel)

        if (req.LugarId is int lugarId)
        {
            if (!await _repo.LugarExisteAsync(lugarId, ct))
                throw new ReglaNegocioException("NO_ENCONTRADO", ReglasReporte.MensajeLugarNoExiste);
            if (await _repo.TienePendienteDelLugarAsync(usuarioId, lugarId, ct))
                throw new ReglaNegocioException("YA_REPORTADO", ReglasReporte.MensajeYaReportado);
        }

        if (await _repo.ContarPendientesAsync(usuarioId, ct) >= ReglasReporte.MaximoPendientesPorEstudiante)
            throw new ReglaNegocioException("DEMASIADOS_PENDIENTES", ReglasReporte.MensajeDemasiados);

        var id = await _repo.CrearAsync(usuarioId, motivo, req.LugarId, descripcion, ct);
        return new ReporteCreadoDto(id, ReglasReporte.Pendiente);
    }
}
