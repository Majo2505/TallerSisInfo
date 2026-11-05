using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Breaku.Domain.Rules;

/// <summary>
/// HU10: el resumen que el estudiante confirma. La extracción (HU11) lo guarda ya validado en
/// log_auditoria_rag.datos_extraidos y la creación de la actividad usa ESE resumen: lo que se crea es exactamente lo que se
/// mostró y se confirmó, y no lo que mande el navegador.
/// </summary>
public static class ReglasResumenActividad
{
    public const string MensajeResumenDistinto = "Los datos no coinciden con el resumen que confirmaste. Revísalo y vuelve a confirmar.";
    public const string MensajeOtroDia = "Ese resumen es de otro día. Vuelve a escribir tu actividad.";

    private sealed record Json(
        [property: JsonPropertyName("actividad")] string? Actividad,
        [property: JsonPropertyName("horaInicio")] string? HoraInicio,
        [property: JsonPropertyName("duracionMin")] int? DuracionMin,
        [property: JsonPropertyName("maxParticipantes")] int? MaxParticipantes);

    /// <summary>El resumen como JSON para guardar: <c>{"actividad":…,"horaInicio":"HH:mm","duracionMin":…,"maxParticipantes":…}</c>.</summary>
    public static string Serializar(DatosActividad datos)
        => JsonSerializer.Serialize(new Json(datos.Actividad, datos.HoraInicio?.ToString("HH:mm", CultureInfo.InvariantCulture),
            datos.DuracionMin, datos.MaxParticipantes));

    /// <summary>El resumen guardado, o <c>null</c> si no hay (extracciones anteriores a la migración 013) o no se puede leer.</summary>
    public static DatosActividad? Leer(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var j = JsonSerializer.Deserialize<Json>(json);
            return j is null ? null : new DatosActividad(j.Actividad, ReglasExtraccion.LeerHora(j.HoraInicio), j.DuracionMin, j.MaxParticipantes);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>true si lo que se pide crear es lo mismo que el resumen (la actividad, sin distinguir mayúsculas).</summary>
    public static bool Coinciden(DatosActividad resumen, DatosActividad pedido)
        => string.Equals(resumen.Actividad?.Trim(), pedido.Actividad?.Trim(), StringComparison.OrdinalIgnoreCase)
           && resumen.HoraInicio == pedido.HoraInicio
           && resumen.DuracionMin == pedido.DuracionMin
           && resumen.MaxParticipantes == pedido.MaxParticipantes;

    /// <summary>Las actividades son para hoy: el resumen tiene que ser de hoy en hora de Bolivia.</summary>
    public static bool EsDeHoy(DateTime creadoUtc, DateTime ahoraUtc)
        => ReglasNotificacion.AHoraLocal(creadoUtc).Date == ReglasNotificacion.AHoraLocal(ahoraUtc).Date;
}
