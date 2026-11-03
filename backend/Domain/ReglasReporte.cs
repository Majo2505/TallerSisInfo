namespace Breaku.Domain.Rules;

/// <summary>
/// Reglas puras de la revisión de reportes (HU16). Un reporte nace <c>PENDIENTE</c> y el administrador lo cierra una sola
/// vez: <c>RESUELTO</c> (se atendió) o <c>DESCARTADO</c> (no correspondía). Lo mismo exige la base con ck_rep_estado y
/// ck_rep_revision (al cerrarlo, revisor y fecha son obligatorios).
/// </summary>
public static class ReglasReporte
{
    public const string Pendiente = "PENDIENTE";
    public const string Resuelto = "RESUELTO";
    public const string Descartado = "DESCARTADO";

    public const string MensajeDecisionInvalida = "El estado tiene que ser RESUELTO o DESCARTADO.";
    public const string MensajeNoExiste = "El reporte no existe.";
    public const string MensajeYaRevisado = "Este reporte ya fue revisado.";

    /// <summary>La decisión del administrador en mayúsculas y sin espacios, o <c>null</c> si no es RESUELTO ni DESCARTADO.</summary>
    public static string? LeerDecision(string? estado)
    {
        var limpio = estado?.Trim().ToUpperInvariant();
        return limpio is Resuelto or Descartado ? limpio : null;
    }

    /// <summary>Solo un reporte pendiente se puede revisar: una decisión no se cambia después.</summary>
    public static bool SePuedeRevisar(string estadoActual) => estadoActual == Pendiente;
}
