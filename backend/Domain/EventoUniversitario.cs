namespace Breaku.Domain;

public sealed class EventoUniversitario
{
    public long Id { get; set; }
    public string Fuente { get; set; } = "";
    public string FuenteUrl { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
    public string? Lugar { get; set; }
    public DateTime ActualizadoEn { get; set; }
}
