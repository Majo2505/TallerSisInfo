namespace Breaku.Domain;

/// <summary>HU28: una sanción del administrador a un estudiante (tabla <c>sancion</c>).</summary>
public class Sancion
{
    public const string TipoAdvertencia = "ADVERTENCIA";
    public const string TipoSuspension = "SUSPENSION";
    public const string TipoBaja = "BAJA";

    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Tipo { get; set; } = "";
    public string Motivo { get; set; } = "";
    public int? ReporteId { get; set; }
    public int CreadoPor { get; set; }
    public DateTime CreadoEn { get; set; }
    /// <summary>Fin de la suspensión (UTC). Solo la SUSPENSION la tiene, y siempre.</summary>
    public DateTime? Hasta { get; set; }
    public DateTime? LevantadaEn { get; set; }
    public int? LevantadaPor { get; set; }
}
