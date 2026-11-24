namespace Breaku.Domain;

public class ComunidadEfimera
{
    public const string EstadoActiva = "ACTIVA";
    public const string EstadoArchivada = "ARCHIVADA";
    /// <summary>HU35: el admin la quitó por moderación (migración 034). Se puede restaurar; el cierre automático no la toca.</summary>
    public const string EstadoBajada = "BAJADA";

    public int Id { get; set; }
    public int CreadorId { get; set; }

    public long? LogId { get; set; }

    public string TextoOriginal { get; set; } = "";
    public string Actividad { get; set; } = "";

    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }

    public short MaxParticipantes { get; set; }

    public int? LugarId { get; set; }

    /// <summary>
    /// Plan armado con una conexión de «Libres contigo» (migración 032): no aparece en Descubrir, nadie entra sin
    /// invitación y no se buscan invitados automáticos.
    /// </summary>
    public bool Privada { get; set; }

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
