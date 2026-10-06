namespace Breaku.Domain;

/// <summary>Un bloque de clase del estudiante (tabla horario).</summary>
public class Horario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Materia { get; set; } = "";
    public int DiaSemana { get; set; }          // 1 = lunes ... 6 = sábado
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public string? Aula { get; set; }
    public string Origen { get; set; } = "MANUAL"; // MANUAL | FOTO
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
