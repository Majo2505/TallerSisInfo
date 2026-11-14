namespace Breaku.Application.Matching;

public record OpcionTestDto(string Id, string Texto);

public record PreguntaTestDto(string Id, string Bloque, string Texto, string Tipo, int Maximo, bool Opcional, List<OpcionTestDto> Opciones);

/// <summary>Las preguntas del test y, si ya lo hizo, sus respuestas (para poder editarlas).</summary>
public record TestAfinidadDto(
    int Version,
    int MinutosEstimados,
    List<PreguntaTestDto> Preguntas,
    bool Completado,
    Dictionary<string, List<string>>? Respuestas,
    DateTime? ActualizadoEn
);

public record GuardarTestRequest(Dictionary<string, List<string>?>? Respuestas);

/// <summary>Cómo está la conexión con un compañero, vista desde quien consulta.</summary>
public record ConexionResumenDto(int Id, string Estado, bool EnviadaPorMi);

/// <summary>
/// Una conexión. Antes de aceptarse solo trae el nombre corto; aceptada, también el nombre completo y el correo.
/// Un saludo que la otra persona rechazó se le muestra a quien lo envió como PENDIENTE (el rechazo es silencioso).
/// </summary>
public record ConexionDto(
    int Id,
    string Estado,
    bool EnviadaPorMi,
    string Nombre,
    string? Carrera,
    int? Semestre,
    int Compatibilidad,
    List<string> EnComun,
    int DiaSemana,
    string HoraInicio,
    string HoraFin,
    string? Correo,
    DateTime CreadaEn
);

public record ConexionesDto(List<ConexionDto> Recibidas, List<ConexionDto> Enviadas, List<ConexionDto> Aceptadas);

public record SaludarRequest(int? UsuarioId, int? PuenteId);

public record ResponderConexionRequest(bool? Aceptar);
