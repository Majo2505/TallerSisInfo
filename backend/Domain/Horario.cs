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

    /// <summary>FALSE cuando el estudiante no asiste a este bloque: no cuenta como tiempo ocupado ni choca con otros.</summary>
    public bool Asiste { get; set; } = true;

    /// <summary>El estudiante confirmó que su carrera autoriza que este bloque coincida con otro.</summary>
    public bool AutorizacionChoque { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
