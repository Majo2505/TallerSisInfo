namespace Breaku.Domain.Rules;

/// <summary>
/// Reglas puras de Comunidades → Descubrir (adición a HU11): las actividades activas de hoy. No dependen de nada.
/// Unirse a una comunidad (RSVP) no es parte de esto: es de HU14.
/// </summary>
public static class ReglasDescubrir
{
    public const string EstadoOk = "OK";
    public const string EstadoSinResultados = "SIN_RESULTADOS";
    public const string EstadoSinZona = "SIN_ZONA";
    public const string EstadoNoVisible = "NO_VISIBLE";

    public const string MensajeSinResultados = "Hoy todavía no hay comunidades activas en tu campus. ¡Crea la primera!";
    public const string MensajeSinZona = "Elige tu campus para ver las comunidades de tus compañeros.";
    public const string MensajeNoVisible = "Para ver las comunidades de tus compañeros, primero acepta que ellos también te encuentren.";

    /// <summary>Cuántas comunidades se muestran como máximo.</summary>
    public const int Maximo = 50;

    /// <summary>
    /// Lo único que se muestra de quien creó la comunidad: el nombre y la inicial del apellido («Ana Pérez» → «Ana P.»).
    /// </summary>
    public static string NombreCorto(string? nombre)
    {
        var partes = (nombre ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length switch
        {
            0 => "Un compañero",
            1 => partes[0],
            _ => $"{partes[0]} {char.ToUpperInvariant(partes[1][0])}."
        };
    }

    /// <summary>
    /// Personas que ya están en la comunidad. Quien la crea siempre está dentro: HU13 lo guarda como CONFIRMADO,
    /// pero si la invitación falló puede no tener su fila en participacion, y igual se cuenta.
    /// </summary>
    public static int Confirmados(int confirmadosEnTabla, bool creadorEnTabla)
        => confirmadosEnTabla + (creadorEnTabla ? 0 : 1);

    /// <summary>
    /// Qué estado y qué mensaje se muestran. Las propias y aquellas a las que invitaron al estudiante se ven siempre;
    /// las de los demás, solo con campus y con el mismo consentimiento recíproco que el matching (HU12).
    /// </summary>
    public static (string Estado, string? Mensaje) Estado(bool tieneZona, bool visible, int cantidad)
    {
        if (!tieneZona) return (EstadoSinZona, MensajeSinZona);
        if (!visible) return (EstadoNoVisible, MensajeNoVisible);
        return cantidad == 0 ? (EstadoSinResultados, MensajeSinResultados) : (EstadoOk, null);
    }
}
