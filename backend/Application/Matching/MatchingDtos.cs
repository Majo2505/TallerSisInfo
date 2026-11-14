using Breaku.Application.Puentes;

namespace Breaku.Application.Matching;

public record ConfiguracionMatchingDto(bool Visible, DateTime? Desde);

public record ActualizarConfiguracionMatchingRequest(bool? Visible);

/// <summary>
/// Un compañero que coincide contigo en tiempo libre y en el test. Por privacidad, solo el nombre corto
/// («Ana Q.»); el nombre completo y el correo aparecen recién cuando los dos aceptan conectar.
/// </summary>
/// <param name="Porcentaje">Qué parte de tu puente comparten (0–100).</param>
/// <param name="Afinidad">Qué tanto se parecen según el test (0–100).</param>
/// <param name="Compatibilidad">Lo que se muestra: 60 % test y 40 % tiempo libre.</param>
/// <param name="EnComun">Lo que más los conecta, hasta 3 cosas.</param>
public record CompaneroCompatibleDto(
    int UsuarioId,
    string Nombre,
    string? Carrera,
    int? Semestre,
    string HoraInicio,
    string HoraFin,
    int MinutosEnComun,
    int Porcentaje,
    int FranjasCompartidas,
    int Afinidad,
    int Compatibilidad,
    List<string> EnComun,
    ConexionResumenDto? Conexion
);

public record ResultadoMatchingDto(
    string Estado,
    string? Mensaje,
    PuenteDto Puente,
    int MinutosPuente,
    List<CompaneroCompatibleDto> Companeros
);
