using System.ComponentModel.DataAnnotations;

namespace Breaku.Application.Ubicacion;

public sealed record ZonaDto(int Id, string Nombre);

public sealed record EstadoUbicacionDto(
    bool ConsentimientoActivo,
    ZonaDto? ZonaActual
);

public sealed record CompartirUbicacionRequest(
    [Range(typeof(decimal), "-90", "90")] decimal Latitud,
    [Range(typeof(decimal), "-180", "180")] decimal Longitud
);

public sealed record SeleccionarZonaRequest(
    [Range(1, int.MaxValue)] int ZonaId
);
