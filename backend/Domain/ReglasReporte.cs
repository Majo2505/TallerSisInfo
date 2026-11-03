namespace Breaku.Domain.Rules;

public static class ReglasReporte
{
    public const string Pendiente = "PENDIENTE";
    public const string Resuelto = "RESUELTO";
    public const string Descartado = "DESCARTADO";

    public const string MensajeDecisionInvalida = "El estado tiene que ser RESUELTO o DESCARTADO.";
    public const string MensajeNoExiste = "El reporte no existe.";
    public const string MensajeYaRevisado = "Este reporte ya fue revisado.";

    public static string? LeerDecision(string? estado)
    {
        var limpio = estado?.Trim().ToUpperInvariant();
        return limpio is Resuelto or Descartado ? limpio : null;
    }

    public static bool SePuedeRevisar(string estadoActual) => estadoActual == Pendiente;
}
