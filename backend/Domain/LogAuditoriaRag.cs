namespace Breaku.Domain;

public class LogAuditoriaRag
{
    public const string TipoRecomendacion = "RECOMENDACION";
    public const string TipoExtraccionComunidad = "EXTRACCION_COMUNIDAD";

    public const string EstadoOk = "OK";
    public const string EstadoSinResultados = "SIN_RESULTADOS";
    public const string EstadoRechazadoSeguridad = "RECHAZADO_SEGURIDAD";
    public const string EstadoError = "ERROR";
    public const string EstadoIncompleto = "INCOMPLETO";

    public long Id { get; set; }
    public int UsuarioId { get; set; }
    public string Tipo { get; set; } = "";
    public int? ZonaId { get; set; }
    public int? RadioAplicadoM { get; set; }
    public int? PuenteId { get; set; }
    public short? TiempoDisponibleMin { get; set; }
    public string? EntradaUsuario { get; set; }
    public string? PromptEnviado { get; set; }
    public string? RespuestaLlm { get; set; }
    public string Estado { get; set; } = "";
    public string? MotivoRechazo { get; set; }
    public string? Modelo { get; set; }
    public int? LatenciaMs { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<LugarRecuperado> LugaresRecuperados { get; set; } = new();
}
