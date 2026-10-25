namespace Breaku.Domain;

public class Zona
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public int RadioMaxM { get; set; }
}
