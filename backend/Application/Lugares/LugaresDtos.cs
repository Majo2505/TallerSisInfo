using Breaku.Application.Ubicacion;

namespace Breaku.Application.Lugares;

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
    List<string> ActividadesSugeridas,
    string? FotoUrl);

public record ResultadoBusquedaLugaresDto(
    string Estado,
    string? Mensaje,
    ZonaDto? Zona,
    int? RadioAplicadoM,
    int MinutosDisponibles,
    List<LugarViableDto> Lugares);
