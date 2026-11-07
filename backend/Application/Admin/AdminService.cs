using Breaku.Application.Horarios;
using Breaku.Domain.Rules;

namespace Breaku.Application.Admin;

public class AdminService
{
    public static readonly string[] EstadosLugar = ["ACTIVO", "EN_REVISION", "INACTIVO"];
    public const int LimiteConsultasPorDefecto = 50;
    public const int LimiteConsultasMaximo = 200;

    private readonly IAdminRepository _repo;
    public AdminService(IAdminRepository repo) => _repo = repo;

    public Task<ResumenAdminDto> ResumenAsync(CancellationToken ct = default) => _repo.ResumenAsync(ct);

    public Task<List<LugarAdminDto>> ListarLugaresAsync(CancellationToken ct = default) => _repo.ListarLugaresAsync(ct);

    public async Task CambiarEstadoLugarAsync(int lugarId, CambioEstadoLugarDto req, int adminId, CancellationToken ct = default)
    {
        var estado = req.Estado?.Trim().ToUpperInvariant() ?? "";
        if (!EstadosLugar.Contains(estado))
            throw new ReglaNegocioException("VALIDACION", "El estado tiene que ser ACTIVO, EN_REVISION o INACTIVO.");

        var anterior = await _repo.CambiarEstadoLugarAsync(lugarId, estado, adminId, ct);
        if (anterior is null) throw new ReglaNegocioException("NO_ENCONTRADO", "El lugar no existe.");
    }

    public async Task CambiarValidacionLugarAsync(int lugarId, CambioValidacionLugarDto req, int adminId, CancellationToken ct = default)
    {
        if (req.Validado is not bool validado)
            throw new ReglaNegocioException("VALIDACION", "Indica si el lugar está validado.");

        var anterior = await _repo.CambiarValidacionLugarAsync(lugarId, validado, adminId, ct);
        if (anterior is null) throw new ReglaNegocioException("NO_ENCONTRADO", "El lugar no existe.");
    }

    public Task<List<CategoriaAdminDto>> ListarCategoriasAsync(CancellationToken ct = default) => _repo.ListarCategoriasAsync(ct);

    public Task<List<CampusAdminDto>> ListarCampusAsync(CancellationToken ct = default) => _repo.ListarCampusActivosAsync(ct);

    public async Task<LugarDetalleAdminDto> ObtenerLugarAsync(int lugarId, CancellationToken ct = default)
        => await _repo.ObtenerLugarAsync(lugarId, ct) ?? throw new ReglaNegocioException("NO_ENCONTRADO", "El lugar no existe.");

    /// <summary>Crea un lugar en el catálogo: queda ACTIVO y validado (lo cargó el admin) y se puede recomendar.</summary>
    public async Task<LugarCreadoDto> CrearLugarAsync(GuardarLugarDto req, int adminId, CancellationToken ct = default)
    {
        var datos = await ValidarLugarAsync(req, null, ct);
        return new LugarCreadoDto(await _repo.CrearLugarAsync(datos, adminId, ct));
    }

    public async Task EditarLugarAsync(int lugarId, GuardarLugarDto req, int adminId, CancellationToken ct = default)
    {
        var datos = await ValidarLugarAsync(req, lugarId, ct);
        if (!await _repo.EditarLugarAsync(lugarId, datos, adminId, ct))
            throw new ReglaNegocioException("NO_ENCONTRADO", "El lugar no existe.");
    }

    /// <summary>
    /// Elimina un lugar que nunca se usó (por ejemplo, uno cargado por error). Si ya aparece en recomendaciones, reportes o
    /// comunidades, borrarlo rompería ese historial: se responde EN_USO y se sugiere desactivarlo.
    /// </summary>
    public async Task EliminarLugarAsync(int lugarId, int adminId, CancellationToken ct = default)
    {
        if (await _repo.ObtenerLugarAsync(lugarId, ct) is null) throw new ReglaNegocioException("NO_ENCONTRADO", "El lugar no existe.");
        if (await _repo.LugarEnUsoAsync(lugarId, ct)) throw new ReglaNegocioException("EN_USO", ReglasLugar.MensajeEnUso);
        if (!await _repo.EliminarLugarAsync(lugarId, adminId, ct)) throw new ReglaNegocioException("NO_ENCONTRADO", "El lugar no existe.");
    }

    private async Task<DatosLugar> ValidarLugarAsync(GuardarLugarDto req, int? lugarId, CancellationToken ct)
    {
        var apertura = LeerHora(req.HoraApertura);
        var cierre = LeerHora(req.HoraCierre);
        var datos = new DatosLugar(ReglasLugar.LimpiarNombre(req.Nombre), ReglasLugar.LimpiarDescripcion(req.Descripcion),
            req.CategoriaId, req.Latitud, req.Longitud, req.DuracionEstimadaMin, apertura, cierre, req.MinAPieDesdeBloqueA);

        var campus = (await _repo.ListarCampusActivosAsync(ct))
            .Select(c => new CampusRef(c.Nombre, c.Latitud, c.Longitud, c.RadioMaxM)).ToList();
        var error = ReglasLugar.Validar(datos, campus);
        if (error is not null) throw new ReglaNegocioException("VALIDACION", error);

        if (!(await _repo.ListarCategoriasAsync(ct)).Any(c => c.Id == datos.CategoriaId))
            throw new ReglaNegocioException("VALIDACION", ReglasLugar.MensajeCategoria);
        if (await _repo.NombreEnUsoAsync(datos.Nombre!, lugarId, ct))
            throw new ReglaNegocioException("NOMBRE_REPETIDO", ReglasLugar.MensajeNombreRepetido);
        return datos;

        // Una hora que vino pero no se puede leer cuenta como horario inválido (no como «sin horario»).
        TimeOnly? LeerHora(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;
            return ReglasExtraccion.LeerHora(texto) ?? throw new ReglaNegocioException("VALIDACION", ReglasLugar.MensajeHorario);
        }
    }

    public Task<List<ReporteAdminDto>> ListarReportesAsync(CancellationToken ct = default) => _repo.ListarReportesAsync(ct);

    public async Task ResolverReporteAsync(int reporteId, ResolverReporteDto req, int adminId, CancellationToken ct = default)
    {
        var decision = ReglasReporte.LeerDecision(req.Estado)
                       ?? throw new ReglaNegocioException("VALIDACION", ReglasReporte.MensajeDecisionInvalida);

        var anterior = await _repo.ResolverReporteAsync(reporteId, decision, adminId, ct)
                       ?? throw new ReglaNegocioException("NO_ENCONTRADO", ReglasReporte.MensajeNoExiste);
        if (!ReglasReporte.SePuedeRevisar(anterior))
            throw new ReglaNegocioException("YA_REVISADO", ReglasReporte.MensajeYaRevisado);
    }

    public async Task<List<ConsultaRagAdminDto>> ListarConsultasRagAsync(int? limite, CancellationToken ct = default)
        => AuditoriaConversaciones.Agrupar(
            await _repo.ListarConsultasRagAsync(Math.Clamp(limite ?? LimiteConsultasPorDefecto, 1, LimiteConsultasMaximo), ct));

    public Task<MetricasAdminDto> MetricasAsync(CancellationToken ct = default) => _repo.MetricasAsync(ct);
}
