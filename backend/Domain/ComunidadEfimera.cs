namespace Breaku.Domain;

public class ComunidadEfimera
{
    public const string EstadoActiva = "ACTIVA";
    public const string EstadoArchivada = "ARCHIVADA";

    public int Id { get; set; }
    public int CreadorId { get; set; }

    public long? LogId { get; set; }

    public string TextoOriginal { get; set; } = "";
    public string Actividad { get; set; } = "";

    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }

    public short MaxParticipantes { get; set; }

    public int? LugarId { get; set; }
    public string Estado { get; set; } = EstadoActiva;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Participacion
{
    public const string EstadoPendiente = "PENDIENTE";
    public const string EstadoConfirmado = "CONFIRMADO";
    public const string EstadoRechazado = "RECHAZADO";

    public int ComunidadId { get; set; }
    public int UsuarioId { get; set; }
    public string Estado { get; set; } = EstadoPendiente;
    public DateTime InvitadoEn { get; set; }
    public DateTime? RespondidoEn { get; set; }
}
