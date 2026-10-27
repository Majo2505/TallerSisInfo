namespace Breaku.Domain;

/// <summary>
/// Recomendación que el asistente generó y se mostró al estudiante (tabla recomendacion).
/// La FK (log_id, lugar_id) → lugar_recuperado impide en la base de datos recomendar un lugar
/// que no se recuperó en ESA misma consulta (RN04 / RNF06).
/// </summary>
public class Recomendacion
{
    public long Id { get; set; }
    public long LogId { get; set; }
    public int LugarId { get; set; }
    public string Explicacion { get; set; } = "";
    public string? Feedback { get; set; }               // LIKE | DISLIKE (fuera del alcance de HU7)
    public DateTime? FechaFeedback { get; set; }

    public LugarRecuperado LugarRecuperado { get; set; } = null!;
}
