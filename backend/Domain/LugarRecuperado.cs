namespace Breaku.Domain;

public class LugarRecuperado
{
    public long LogId { get; set; }
    public int LugarId { get; set; }
    public int DistanciaM { get; set; }
    public short TiempoTrasladoMin { get; set; }
    public short Orden { get; set; }

    public LogAuditoriaRag Log { get; set; } = null!;
}
