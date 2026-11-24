namespace Breaku.Domain;

/// <summary>
/// HU35: el admin quita una comunidad inapropiada (queda BAJADA, sin borrar datos) y puede restaurarla. Quitar NO sanciona a
/// quien la creó: son decisiones independientes (sancionar es HU28).
/// </summary>
public static class ReglasModeracionComunidad
{
    public const int MaximoMotivo = 500;
    private const int MaximoAviso = 200;
    private const int MaximoRespuesta = 500;

    public const string ErrorValidacion = "VALIDACION";
    public const string ErrorNoEncontrada = "NO_ENCONTRADO";
    /// <summary>Quitar una que no está ACTIVA (ya bajada o archivada), o restaurar una que no está BAJADA.</summary>
    public const string ErrorEstado = "ESTADO_INVALIDO";
    public const string ErrorReporteYaRevisado = "YA_REVISADO";

    public const string MensajeFaltaMotivo = "Escribe el motivo para quitar la comunidad.";
    public const string MensajeMotivoLargo = "El motivo no puede superar 500 caracteres.";
    public const string MensajeNoEncontrada = "La comunidad no existe.";
    public const string MensajeNoActiva = "Solo se puede quitar una comunidad activa (esta ya fue quitada o se archivó).";
    public const string MensajeNoBajada = "Solo se puede restaurar una comunidad que fue quitada.";
    public const string MensajeReporteNoCorresponde = "El reporte no existe o no es sobre esta comunidad.";
    public const string MensajeReporteYaRevisado = "Ese reporte ya fue revisado.";

    /// <summary>El motivo limpio, o el error. Es obligatorio y llega a 500 caracteres.</summary>
    public static (string? Motivo, string? Error) ValidarMotivo(string? motivo)
    {
        var limpio = (motivo ?? "").Trim();
        if (limpio.Length == 0) return (null, MensajeFaltaMotivo);
        if (limpio.Length > MaximoMotivo) return (null, MensajeMotivoLargo);
        return (limpio, null);
    }

    /// <summary>Aviso a quien la creó y a quienes iban: el motivo, recortado para caber en 200 caracteres.</summary>
    public static string AvisoBajada(string actividad, string motivo)
        => Recortar($"«{Recortar(actividad.Trim(), 60)}» fue retirada por moderación. Motivo: {motivo.Trim().ReplaceLineEndings(" ")}", MaximoAviso);

    public static string AvisoRestaurada(string actividad)
        => $"«{Recortar(actividad.Trim(), 120)}» volvió a estar disponible.";

    /// <summary>La respuesta que queda en el reporte (y que ve quien reportó en «Mis reportes») si se quitó desde ahí.</summary>
    public static string RespuestaReporte(string motivo)
        => Recortar($"Quitamos la comunidad. Motivo: {motivo.Trim()}", MaximoRespuesta);

    private static string Recortar(string texto, int maximo)
        => texto.Length <= maximo ? texto : texto[..(maximo - 1)].TrimEnd() + "…";
}
