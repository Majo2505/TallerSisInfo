namespace Breaku.Domain;

public class Horario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Materia { get; set; } = null!;
    public short DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public string? Aula { get; set; }
    public string Origen { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
