using Breaku.Application.Puentes;

namespace Breaku.Application.Matching;

/// <summary>Si el estudiante aceptó que compañeros con tiempo compatible lo encuentren (HU12).</summary>
/// <param name="Visible" example="true">Si aparece en el matching de sus compañeros de sede.</param>
/// <param name="Desde" example="2026-10-31T14:00:04.512Z">Cuándo lo aceptó (UTC), o <c>null</c> si no está visible.</param>
public record ConfiguracionMatchingDto(bool Visible, DateTime? Desde);

/// <summary>Cambio del consentimiento para aparecer en el matching.</summary>
/// <param name="Visible" example="true">Obligatorio. <c>true</c> para aparecer, <c>false</c> para dejar de aparecer.</param>
public record ActualizarConfiguracionMatchingRequest(bool? Visible);

/// <summary>Compañero de la misma sede con un puente compatible. No incluye correo ni identificador.</summary>
/// <param name="Nombre" example="Ana Quiroga">Nombre del compañero.</param>
/// <param name="Carrera" example="Ingeniería de Sistemas">Carrera, o <c>null</c> si no la completó.</param>
/// <param name="Semestre" example="5">Semestre, o <c>null</c>.</param>
/// <param name="HoraInicio" example="10:00:00">Inicio del tramo en común (<c>HH:mm:ss</c>).</param>
/// <param name="HoraFin" example="11:00:00">Fin del tramo en común (<c>HH:mm:ss</c>).</param>
/// <param name="MinutosEnComun" example="60">Duración del tramo en común. Siempre 30 o más.</param>
/// <param name="Porcentaje" example="67">Qué parte de tu puente cubre el tramo en común, de 1 a 100.</param>
/// <param name="FranjasCompartidas" example="2">En cuántos de tus puentes de la semana coinciden (incluye este).</param>
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

/// <summary>Resultado del matching para uno de tus puentes.</summary>
/// <param name="Estado" example="OK"><c>OK</c>, <c>SIN_RESULTADOS</c>, <c>SIN_ZONA</c> (no eligió campus) o <c>NO_VISIBLE</c> (no aceptó aparecer).</param>
/// <param name="Mensaje" example="Por ahora nadie de tu campus está libre contigo en este puente.">Texto para mostrar cuando no hay compañeros; <c>null</c> si <c>Estado</c> = <c>OK</c>.</param>
/// <param name="Puente">El puente consultado.</param>
/// <param name="MinutosPuente" example="90">Duración del puente consultado.</param>
/// <param name="Companeros">Ordenados por compatibilidad: más minutos en común primero; en empate, más franjas compartidas. Hasta 20.</param>
public record ResultadoMatchingDto(
    string Estado,
    string? Mensaje,
    PuenteDto Puente,
    int MinutosPuente,
    List<CompaneroCompatibleDto> Companeros
);
