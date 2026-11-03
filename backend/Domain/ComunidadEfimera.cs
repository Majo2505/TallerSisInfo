namespace Breaku.Domain;

public class ComunidadEfimera
{
    public int Id { get; set; }
    public int CreadorId { get; set; }
    public string TextoOriginal { get; set; } = "";
    public string Actividad { get; set; } = "";
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
    public short MaxParticipantes { get; set; }
    public int? LugarId { get; set; }
    public string Estado { get; set; } = "ACTIVA";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Participacion
{
    public int ComunidadId { get; set; }
    public int UsuarioId { get; set; }
    public string Estado { get; set; } = "PENDIENTE";
    public DateTime InvitadoEn { get; set; }
    public DateTime? RespondidoEn { get; set; }
}
