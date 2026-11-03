namespace Breaku.Domain;

public class Recomendacion
{
    public long Id { get; set; }
    public long LogId { get; set; }
    public int LugarId { get; set; }
    public string Explicacion { get; set; } = "";
    public string? Feedback { get; set; }
    public DateTime? FechaFeedback { get; set; }

    public LugarRecuperado LugarRecuperado { get; set; } = null!;
}
