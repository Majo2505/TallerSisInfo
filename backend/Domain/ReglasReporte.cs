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

    // ---------- Reportes que envía el estudiante desde su perfil ----------

    public const string MotivoErrorApp = "ERROR_APP";
    public const string MotivoSugerencia = "SUGERENCIA";
    public const string MotivoLugarIncorrecto = "LUGAR_INCORRECTO";

    /// <summary>Los motivos que se pueden enviar desde el perfil (COMUNIDAD_INAPROPIADA lo admite la base para más adelante).</summary>
    public static readonly string[] MotivosDelEstudiante = [MotivoErrorApp, MotivoSugerencia, MotivoLugarIncorrecto];

    public const int LargoMinimoDescripcion = 10;
    public const int LargoMaximoDescripcion = 500;
    public const int MaximoPendientesPorEstudiante = 3;

    public const string MensajeMotivo = "Elige el tipo de reporte.";
    public const string MensajeFaltaLugar = "Elige el lugar que tiene la información incorrecta.";
    public const string MensajeLugarDeMas = "Solo los reportes de información incorrecta van con un lugar.";
    public const string MensajeDescripcionCorta = "Cuéntanos un poco más: al menos 10 caracteres.";
    public const string MensajeDescripcionLarga = "Puedes escribir hasta 500 caracteres.";
    public const string MensajeLugarNoExiste = "Ese lugar no existe.";
    public const string MensajeYaReportado = "Ya reportaste este lugar y todavía lo estamos revisando.";
    public const string MensajeDemasiados = "Ya tienes 3 reportes pendientes. Espera a que los revisemos antes de enviar otro.";

    /// <summary>El motivo en mayúsculas, o <c>null</c> si no es uno de los que puede enviar el estudiante.</summary>
    public static string? LeerMotivo(string? motivo)
    {
        var limpio = motivo?.Trim().ToUpperInvariant();
        return limpio is not null && MotivosDelEstudiante.Contains(limpio) ? limpio : null;
    }

    /// <summary>El primer problema del reporte, o <c>null</c> si se puede guardar. El lugar va solo con LUGAR_INCORRECTO.</summary>
    public static string? ValidarNuevo(string? motivo, int? lugarId, string? descripcion)
    {
        var m = LeerMotivo(motivo);
        if (m is null) return MensajeMotivo;
        if (m == MotivoLugarIncorrecto && lugarId is null or <= 0) return MensajeFaltaLugar;
        if (m != MotivoLugarIncorrecto && lugarId is not null) return MensajeLugarDeMas;

        var largo = descripcion?.Trim().Length ?? 0;
        if (largo < LargoMinimoDescripcion) return MensajeDescripcionCorta;
        if (largo > LargoMaximoDescripcion) return MensajeDescripcionLarga;
        return null;
    }
}
