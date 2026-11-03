namespace Breaku.Application.Admin;

public record ResumenAdminDto(
    int LugaresActivos,
    int LugaresEnRevision,
    int Estudiantes,
    int ComunidadesHoy,
    int ConsultasRagSemana,
    int ReportesPendientes
);

public record LugarAdminDto(
    int Id,
    string Nombre,
    string Categoria,
    string Estado,
    int DuracionEstimadaMin,
    string? HoraApertura,
    string? HoraCierre,
    int VecesRecomendado,
    bool Validado = true,
    bool Recomendable = true,
    string? TipoLugar = null
);

public record CambioEstadoLugarDto(string Estado);

public record ReporteAdminDto(
    int Id,
    string Motivo,
    string? Descripcion,
    string Estado,
    string Objeto,
    string Autor,
    DateTime CreadoEn
);

public record ResolverReporteDto(string? Estado);

public record ConsultaRagAdminDto(
    long Id,
    DateTime Fecha,
    string Usuario,
    string Tipo,
    string? Entrada,
    string Estado,
    string? Modelo,
    int? LatenciaMs,
    int LugaresRecuperados,
    int Recomendados,
    string? MotivoRechazo = null
);

public record MetricasAdminDto(
    int ConsultasTotales,
    int Ok,
    int SinResultados,
    int Rechazadas,
    int Errores,
    int? LatenciaPromedioMs,
    List<LugarTopDto> MasRecomendados,
    int MeGusta,
    int NoMeSirve,
    List<RechazoPorMotivoDto>? RechazosPorMotivo = null
);

public record RechazoPorMotivoDto(string Motivo, int Cantidad);

public record LugarTopDto(string Nombre, string Categoria, int Veces, int MeGusta, int NoMeSirve);
