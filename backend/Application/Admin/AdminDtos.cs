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

/// <summary>Decisión del administrador sobre un reporte pendiente.</summary>
/// <param name="Estado" example="RESUELTO"><c>RESUELTO</c> (se atendió) o <c>DESCARTADO</c> (no correspondía). No distingue mayúsculas.</param>
public record ResolverReporteDto(string? Estado);

/// <summary>Una consulta al asistente RAG registrada en el log de auditoría.</summary>
/// <param name="MotivoRechazo">HU17: con estado <c>RECHAZADO_SEGURIDAD</c>, por qué: <c>INYECCION</c>, <c>FUERA_DE_ALCANCE</c>,
/// <c>BIENESTAR</c>, <c>FUERA_DE_REGLAMENTO</c> o <c>FUERA_DE_CONTEXTO</c>. <c>null</c> en los demás (y en rechazos anteriores a la HU17).</param>
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

/// <summary>Métricas del asistente en los últimos 7 días.</summary>
/// <param name="MeGusta">Valoraciones «me gusta» (HU9) registradas en los últimos 7 días.</param>
/// <param name="NoMeSirve">Valoraciones «no me sirve» (HU9) registradas en los últimos 7 días.</param>
/// <param name="RechazosPorMotivo">HU17: rechazos de seguridad de la semana agrupados por motivo (sin motivo = anteriores a la HU17).</param>
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

/// <summary>HU17: cuántas consultas se rechazaron por cada motivo.</summary>
/// <param name="Motivo" example="INYECCION">Categoría del guardrail, o <c>SIN_MOTIVO</c> para los rechazos anteriores a la HU17.</param>
/// <param name="Cantidad" example="3">Consultas rechazadas por ese motivo en los últimos 7 días.</param>
public record RechazoPorMotivoDto(string Motivo, int Cantidad);

/// <summary>Lugar entre los más recomendados, con cómo lo valoraron los estudiantes (HU9).</summary>
public record LugarTopDto(string Nombre, string Categoria, int Veces, int MeGusta, int NoMeSirve);
