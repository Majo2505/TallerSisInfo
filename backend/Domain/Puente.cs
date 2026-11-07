namespace Breaku.Domain;

public class Puente
{
    public const string TipoEntreClases = "ENTRE_CLASES";
    public const string TipoDespuesDeClases = "DESPUES_DE_CLASES";

    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public short DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public string Tipo { get; set; } = TipoEntreClases;
}
