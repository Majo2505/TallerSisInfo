namespace Breaku.Application.Recomendaciones;

public interface IAsistenteIa
{
    Task<RespuestaAsistente> GenerarAsync(PromptRag prompt, CancellationToken ct = default);
}

public record PromptRag(string Sistema, string Usuario)
{
    public string ComoTexto() => $"[SISTEMA]\n{Sistema}\n\n[USUARIO]\n{Usuario}";
}

public record RespuestaAsistente(string Modelo, string TextoCrudo, List<SugerenciaAsistente> Sugerencias);

public record SugerenciaAsistente(int LugarId, string Explicacion);
