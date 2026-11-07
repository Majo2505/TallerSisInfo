namespace Breaku.Domain;

public class Lugar
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }

    public short DuracionEstimadaMin { get; set; }

    public string Estado { get; set; } = string.Empty;

    public string? TipoLugar { get; set; }

    public string? Modalidad { get; set; }

    public string? NivelEnergia { get; set; }

    public short? DuracionMin { get; set; }
    public short? DuracionMax { get; set; }

    public string? ActividadesSugeridas { get; set; }

    public short? MinAPieDesdeBloqueA { get; set; }

    public bool Recomendable { get; set; } = true;

    public bool Validado { get; set; } = true;

    /// <summary>Cuándo cambió la foto real del lugar (versión de su URL). Null: no tiene foto.</summary>
    public DateTime? FotoActualizadaEn { get; set; }
}
