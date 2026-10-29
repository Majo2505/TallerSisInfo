namespace Breaku.Domain;

/// <summary>Categoría de un lugar del catálogo (tabla categoria): Comida, Estudio, Descanso, etc.</summary>
public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
