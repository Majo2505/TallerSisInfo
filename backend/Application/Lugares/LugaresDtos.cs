using Breaku.Application.Ubicacion;

namespace Breaku.Application.Lugares;

/// <summary>Un lugar del catálogo que cabe en el puente del estudiante.</summary>
/// <param name="LugarId" example="2">Id del lugar en el catálogo.</param>
/// <param name="Nombre" example="Biblioteca Central">Nombre del lugar.</param>
/// <param name="Categoria" example="Estudio">Categoría del lugar.</param>
/// <param name="Descripcion" example="Salas silenciosas y computadoras para estudiar.">Descripción opcional del catálogo.</param>
/// <param name="DistanciaM" example="120">Distancia en línea recta desde el campus, en metros.</param>
/// <param name="TiempoTrasladoMin" example="3">Minutos de traslado caminando, de ida y vuelta.</param>
/// <param name="DuracionEstimadaMin" example="45">Minutos que lleva la actividad en el lugar.</param>
/// <param name="Validado" example="false"><c>false</c> si el equipo todavía no revisó los datos del lugar: la app lo muestra como «por validar».</param>
/// <param name="TipoLugar" example="comida">Tipo de lugar según el relevamiento; <c>null</c> si no se cargó.</param>
/// <param name="Modalidad" example="[&quot;solitario&quot;,&quot;grupal&quot;]">Modalidades del lugar; vacía si no se cargó.</param>
/// <param name="NivelEnergia" example="[&quot;bajo&quot;,&quot;medio&quot;]">Niveles de energía que pide el lugar; vacía si no se cargó.</param>
/// <param name="ActividadesSugeridas" example="[&quot;estudiar&quot;,&quot;leer&quot;]">Actividades que se pueden hacer; vacía si no se cargó.</param>
public record LugarViableDto(
    int LugarId,
    string Nombre,
    string Categoria,
    string? Descripcion,
    int DistanciaM,
    int TiempoTrasladoMin,
    int DuracionEstimadaMin,
    bool Validado,
    string? TipoLugar,
    List<string> Modalidad,
    List<string> NivelEnergia,
    List<string> ActividadesSugeridas);

/// <summary>Resultado de buscar lugares viables para un puente.</summary>
/// <param name="Estado"><c>OK</c> (hay lugares), <c>SIN_RESULTADOS</c> (ninguno cumple) o <c>SIN_ZONA</c> (falta elegir el campus).</param>
/// <param name="Mensaje">Texto para mostrar cuando el estado no es <c>OK</c>; <c>null</c> si hay lugares.</param>
/// <param name="Zona">Campus usado como origen de la búsqueda; <c>null</c> con <c>SIN_ZONA</c>.</param>
/// <param name="RadioAplicadoM" example="800">Radio máximo de la zona, en metros; <c>null</c> con <c>SIN_ZONA</c>.</param>
/// <param name="MinutosDisponibles" example="110">Duración del puente consultado, en minutos.</param>
/// <param name="Lugares">Lugares viables, del más cercano al más lejano.</param>
public record ResultadoBusquedaLugaresDto(
    string Estado,
    string? Mensaje,
    ZonaDto? Zona,
    int? RadioAplicadoM,
    int MinutosDisponibles,
    List<LugarViableDto> Lugares);
