namespace Breaku.Application.Reportes;

/// <summary>Reporte que envía el estudiante desde su perfil.</summary>
/// <param name="Motivo" example="LUGAR_INCORRECTO"><c>ERROR_APP</c> (algo no funciona), <c>SUGERENCIA</c> o <c>LUGAR_INCORRECTO</c>.</param>
/// <param name="LugarId" example="4">Solo con <c>LUGAR_INCORRECTO</c>: el lugar (de <c>GET /api/reportes/lugares</c>).</param>
/// <param name="Descripcion" example="El horario dice que abre a las 7, pero abre a las 9.">De 10 a 500 caracteres.</param>
public record NuevoReporteDto(string? Motivo, int? LugarId, string? Descripcion);

/// <summary>Reporte recibido.</summary>
/// <param name="Id" example="12">Identificador del reporte.</param>
/// <param name="Estado" example="PENDIENTE">Siempre <c>PENDIENTE</c>: lo revisa un administrador.</param>
public record ReporteCreadoDto(int Id, string Estado);

/// <summary>Lugar que se puede reportar.</summary>
public record LugarReportableDto(int Id, string Nombre);
