namespace Breaku.Domain;

/// <summary>
/// Registro de auditoría de una consulta al asistente (tabla log_auditoria_rag). HU7 usa el tipo RECOMENDACION.
/// Se guarda SIEMPRE, también cuando no hubo resultados o falló el LLM, para poder rastrear cada respuesta.
/// </summary>
public class LogAuditoriaRag
{
    public const string TipoRecomendacion = "RECOMENDACION";

    public const string EstadoOk = "OK";
    public const string EstadoSinResultados = "SIN_RESULTADOS";
    public const string EstadoRechazadoSeguridad = "RECHAZADO_SEGURIDAD";   // el asistente propuso algo fuera del contexto
    public const string EstadoError = "ERROR";

    public long Id { get; set; }
    public int UsuarioId { get; set; }
    public string Tipo { get; set; } = "";              // RECOMENDACION | EXTRACCION_COMUNIDAD
    public int? ZonaId { get; set; }
    public int? RadioAplicadoM { get; set; }            // copia del radio usado en la búsqueda (RN06)
    public int? PuenteId { get; set; }
    public short? TiempoDisponibleMin { get; set; }     // copia de la duración del puente al momento de la consulta
    public string? EntradaUsuario { get; set; }
    public string? PromptEnviado { get; set; }          // null si nunca se llamó al LLM
    public string? RespuestaLlm { get; set; }
    public string Estado { get; set; } = "";            // OK | SIN_RESULTADOS | RECHAZADO_SEGURIDAD | ERROR
    public string? Modelo { get; set; }
    public int? LatenciaMs { get; set; }
    public DateTime CreatedAt { get; set; }             // UTC

    /// <summary>Lugares que devolvió la búsqueda en el catálogo: el ÚNICO contexto que puede usar el asistente.</summary>
    public List<LugarRecuperado> LugaresRecuperados { get; set; } = new();
}
