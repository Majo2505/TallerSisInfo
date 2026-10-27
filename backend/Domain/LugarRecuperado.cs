namespace Breaku.Domain;

/// <summary>Lugar del catálogo recuperado en una consulta (tabla lugar_recuperado). Clave: (LogId, LugarId).</summary>
public class LugarRecuperado
{
    public long LogId { get; set; }
    public int LugarId { get; set; }
    public int DistanciaM { get; set; }
    public short TiempoTrasladoMin { get; set; }        // ida y vuelta (RN03)
    public short Orden { get; set; }                    // 1 = el más relevante según la búsqueda

    public LogAuditoriaRag Log { get; set; } = null!;
}
