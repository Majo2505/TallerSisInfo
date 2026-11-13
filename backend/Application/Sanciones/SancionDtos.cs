namespace Breaku.Application.Sanciones;

/// <summary>Fila de la lista de usuarios. <c>Sanciones</c> es el total aplicado (historial) y <c>Advertencias</c> las que cuentan (sin las levantadas).</summary>
public record UsuarioListaDto(int Id, string Nombre, string Correo, string? Carrera, string Estado, int Sanciones, int Advertencias);

public record SancionDto(
    int Id,
    string Tipo,
    string Motivo,
    int? ReporteId,
    string CreadoPor,
    DateTime CreadoEn,
    DateTime? Hasta,
    DateTime? LevantadaEn,
    string? LevantadaPor,
    /// <summary>Todavía surte efecto: suspensión sin levantar y sin vencer, o baja.</summary>
    bool Vigente);

public record BloqueoDto(string Tipo, string Motivo, DateTime? Hasta);

public record UsuarioDetalleDto(
    int Id,
    string Nombre,
    string Correo,
    string? Carrera,
    byte? Semestre,
    string Estado,
    DateTime RegistradoEn,
    int Advertencias,
    /// <summary>Solo una sugerencia: tras 3 advertencias la siguiente es SUSPENSION. Nada es automático.</summary>
    string SiguienteSancionSugerida,
    int AdvertenciasParaSuspension,
    int DiasSuspensionPorDefecto,
    BloqueoDto? Bloqueo,
    List<SancionDto> Sanciones);

/// <summary>Lo que manda el administrador. <c>Hasta</c> solo para SUSPENSION (UTC; sin fecha, 7 días).</summary>
public record SancionarUsuarioDto(string? Tipo, string? Motivo, DateTime? Hasta, int? ReporteId);

public record SancionAplicadaDto(SancionDto Sancion, string EstadoUsuario, int Advertencias, string SiguienteSancionSugerida);
