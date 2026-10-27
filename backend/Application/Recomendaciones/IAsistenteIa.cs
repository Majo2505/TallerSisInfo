namespace Breaku.Application.Recomendaciones;

/// <summary>
/// Puerto: el modelo de lenguaje que redacta las recomendaciones. Lo implementa Infrastructure
/// (hoy <c>AsistenteIaSimulado</c>; después, el cliente de OpenAI).
/// </summary>
public interface IAsistenteIa
{
    /// <summary>
    /// Envía el prompt y devuelve las sugerencias ya leídas del JSON de respuesta.
    /// Si el servicio falla, tarda demasiado o responde algo que no se puede leer, lanza una excepción:
    /// RecomendacionService la registra como ERROR en el log.
    /// </summary>
    Task<RespuestaAsistente> GenerarAsync(PromptRag prompt, CancellationToken ct = default);
}

/// <summary>Mensajes que se envían al modelo: instrucciones de sistema y mensaje con el contexto recuperado.</summary>
public record PromptRag(string Sistema, string Usuario)
{
    /// <summary>Forma en que se guarda en log_auditoria_rag.prompt_enviado.</summary>
    public string ComoTexto() => $"[SISTEMA]\n{Sistema}\n\n[USUARIO]\n{Usuario}";
}

/// <param name="Modelo">Nombre del modelo que respondió (se copia al log).</param>
/// <param name="TextoCrudo">Respuesta completa del modelo, sin procesar (se copia al log).</param>
/// <param name="Sugerencias">Sugerencias leídas de la respuesta. SIN validar: las filtra ReglasRecomendacion.</param>
public record RespuestaAsistente(string Modelo, string TextoCrudo, List<SugerenciaAsistente> Sugerencias);

public record SugerenciaAsistente(int LugarId, string Explicacion);
