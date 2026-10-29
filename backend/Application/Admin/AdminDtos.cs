namespace Breaku.Application.Admin;

/// <summary>Cifras del campus para la cabecera del panel de administración.</summary>
public record ResumenAdminDto(
    int LugaresActivos,
    int LugaresEnRevision,
    int Estudiantes,
    int ComunidadesHoy,
    int ConsultasRagSemana,
    int ReportesPendientes
);

/// <summary>Lugar del catálogo tal como lo ve el administrador.</summary>
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

/// <summary>Nuevo estado de un lugar: <c>ACTIVO</c>, <c>EN_REVISION</c> o <c>INACTIVO</c>.</summary>
public record CambioEstadoLugarDto(string Estado);

/// <summary>Reporte de un estudiante sobre un lugar o una comunidad.</summary>
public record ReporteAdminDto(
    int Id,
    string Motivo,
    string? Descripcion,
    string Estado,
    string Objeto,
    string Autor,
    DateTime CreadoEn
);

/// <summary>Una consulta al asistente RAG registrada en el log de auditoría.</summary>
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
    int Recomendados
);

/// <summary>Métricas del asistente en los últimos 7 días.</summary>
public record MetricasAdminDto(
    int ConsultasTotales,
    int Ok,
    int SinResultados,
    int Rechazadas,
    int Errores,
    int? LatenciaPromedioMs,
    List<LugarTopDto> MasRecomendados
);

public record LugarTopDto(string Nombre, string Categoria, int Veces);
