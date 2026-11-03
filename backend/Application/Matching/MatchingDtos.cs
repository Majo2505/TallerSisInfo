using Breaku.Application.Puentes;

namespace Breaku.Application.Matching;

public record ConfiguracionMatchingDto(bool Visible, DateTime? Desde);

public record ActualizarConfiguracionMatchingRequest(bool? Visible);

public record CompaneroCompatibleDto(
    string Nombre,
    string? Carrera,
    int? Semestre,
    string HoraInicio,
    string HoraFin,
    int MinutosEnComun,
    int Porcentaje,
    int FranjasCompartidas
);

public record ResultadoMatchingDto(
    string Estado,
    string? Mensaje,
    PuenteDto Puente,
    int MinutosPuente,
    List<CompaneroCompatibleDto> Companeros
);
