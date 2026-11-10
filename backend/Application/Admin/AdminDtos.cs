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

/// <summary>Datos de un lugar para crearlo o editarlo. Las horas en <c>HH:mm</c>; apertura y cierre van juntas o ninguna.</summary>
/// <param name="Nombre" example="Biblioteca Central">De 3 a 100 caracteres; único en el catálogo.</param>
/// <param name="Descripcion" example="Sala de lectura silenciosa con enchufes.">Opcional, hasta 500 caracteres.</param>
/// <param name="CategoriaId" example="2">De <c>GET /api/admin/categorias</c>.</param>
/// <param name="Latitud" example="-17.3716">Tiene que quedar dentro del radio de un campus activo.</param>
/// <param name="Longitud" example="-66.1439">Ídem.</param>
/// <param name="DuracionEstimadaMin" example="30">De 1 a 240 minutos.</param>
/// <param name="HoraApertura" example="07:30">Opcional (<c>HH:mm</c>).</param>
/// <param name="HoraCierre" example="20:00">Opcional; después de la apertura.</param>
/// <param name="MinAPieDesdeBloqueA" example="4">Opcional, de 0 a 60.</param>
public record GuardarLugarDto(
    string? Nombre,
    string? Descripcion,
    int? CategoriaId,
    decimal? Latitud,
    decimal? Longitud,
    int? DuracionEstimadaMin,
    string? HoraApertura,
    string? HoraCierre,
    int? MinAPieDesdeBloqueA);

/// <summary>Un lugar con todos sus datos editables (para el formulario de edición).</summary>
public record LugarDetalleAdminDto(
    int Id,
    string Nombre,
    string? Descripcion,
    int CategoriaId,
    decimal Latitud,
    decimal Longitud,
    int DuracionEstimadaMin,
    string? HoraApertura,
    string? HoraCierre,
    int? MinAPieDesdeBloqueA,
    string Estado,
    bool Validado,
    string? FotoUrl = null);

/// <summary>Lugar recién creado.</summary>
public record LugarCreadoDto(int Id);

/// <summary>Categoría del catálogo.</summary>
public record CategoriaAdminDto(int Id, string Nombre);

/// <summary>Campus activo: el centro sirve de punto de partida al ubicar un lugar nuevo y el radio, de límite.</summary>
public record CampusAdminDto(int Id, string Nombre, decimal Latitud, decimal Longitud, int RadioMaxM);

public record CambioValidacionLugarDto(bool? Validado);

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
    string? MotivoRechazo = null,
    List<string>? Respuestas = null,
    int Turnos = 1
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
