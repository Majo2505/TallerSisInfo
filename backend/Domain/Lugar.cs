namespace Breaku.Domain;

/// <summary>
/// Lugar del catálogo (tabla lugar). Solo se lee: el alta y la aprobación las hace el administrador.
/// Solo los lugares en estado <c>ACTIVO</c> (verificados y aprobados) pueden mostrarse a los estudiantes.
/// </summary>
public class Lugar
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }

    /// <summary>Minutos que lleva hacer la actividad en el lugar, sin contar el traslado.</summary>
    public short DuracionEstimadaMin { get; set; }

    /// <summary><c>ACTIVO</c>, <c>EN_REVISION</c> o <c>INACTIVO</c>.</summary>
    public string Estado { get; set; } = string.Empty;
}
