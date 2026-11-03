namespace Breaku.Domain;

/// <summary>
/// Grupo que se arma solo para un rato libre (tabla comunidad_efimera). HU13 la crea automáticamente cuando hay
/// compañeros libres a la hora de la actividad que describió el estudiante (HU11).
/// </summary>
public class ComunidadEfimera
{
    public const string EstadoActiva = "ACTIVA";
    public const string EstadoArchivada = "ARCHIVADA";

    public int Id { get; set; }
    public int CreadorId { get; set; }
    public long? LogId { get; set; }                 // la extracción de HU11 que la originó (una comunidad por extracción)
    public string TextoOriginal { get; set; } = "";  // lo que escribió el estudiante
    public string Actividad { get; set; } = "";
    public DateTime Inicio { get; set; }             // UTC
    public DateTime Fin { get; set; }                // UTC
    public short MaxParticipantes { get; set; }      // contando a quien la crea
    public int? LugarId { get; set; }
    public string Estado { get; set; } = EstadoActiva;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Quién está en una comunidad y si respondió (tabla participacion).</summary>
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
