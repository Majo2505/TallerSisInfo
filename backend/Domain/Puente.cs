namespace Breaku.Domain;

public class Puente
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public short DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
}
