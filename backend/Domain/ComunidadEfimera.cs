namespace Breaku.Domain;

/// <summary>Una actividad que un estudiante crea para un rato libre (tabla comunidad_efimera).</summary>
public class ComunidadEfimera
{
    public const string EstadoActiva = "ACTIVA";
    public const string EstadoArchivada = "ARCHIVADA";

    public int Id { get; set; }
    public int CreadorId { get; set; }

    /// <summary>Registro de auditoría de la extracción (HU11) de la que salió la actividad; <c>null</c> si se creó sin extraer.</summary>
    public long? LogId { get; set; }

    public string TextoOriginal { get; set; } = "";
    public string Actividad { get; set; } = "";

    /// <summary>Inicio y fin en UTC, como el resto de las fechas guardadas.</summary>
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }

    /// <summary>Personas en total, contando a quien la crea.</summary>
    public short MaxParticipantes { get; set; }

    public int? LugarId { get; set; }
    public string Estado { get; set; } = EstadoActiva;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Quién está en una comunidad y si respondió (tabla participacion). HU13 la llena al invitar.</summary>
public class Participacion
{
    public const string EstadoPendiente = "PENDIENTE";
    public const string EstadoConfirmado = "CONFIRMADO";
    public const string EstadoRechazado = "RECHAZADO";

    public int ComunidadId { get; set; }
    public int UsuarioId { get; set; }
    public string Estado { get; set; } = EstadoPendiente;
    public DateTime InvitadoEn { get; set; }         // UTC
    public DateTime? RespondidoEn { get; set; }      // obligatorio si ya no está PENDIENTE (ck_part_respuesta)
}
