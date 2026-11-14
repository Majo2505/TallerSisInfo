namespace Breaku.Domain.Rules;

/// <summary>
/// HU12 (ampliación): el «saludo» entre dos estudiantes que coinciden en tiempo libre y en el test. Con privacidad:
/// · Antes de aceptar, cada uno ve del otro solo su nombre corto, carrera, la afinidad y lo que los conecta.
/// · Al aceptar, los dos ven el nombre completo y el correo institucional, para ponerse de acuerdo.
/// · El rechazo es silencioso: quien saludó sigue viendo «saludo enviado», sin enterarse del «no».
/// </summary>
public static class ReglasConexion
{
    public const string Pendiente = "PENDIENTE";
    public const string Aceptada = "ACEPTADA";
    public const string Rechazada = "RECHAZADA";

    /// <summary>Para que nadie salude a todo el campus: hasta 5 saludos sin respuesta a la vez.</summary>
    public const int MaximoPendientesEnviados = 5;

    public const string ErrorYaExiste = "YA_EXISTE";
    public const string ErrorDemasiados = "DEMASIADOS_PENDIENTES";
    public const string ErrorNoCoinciden = "NO_COINCIDEN";
    public const string ErrorYaRespondida = "YA_RESPONDIDA";

    public const string MensajeDemasiados =
        "Ya tienes 5 saludos esperando respuesta. Espera a que te respondan antes de saludar a alguien más.";
    public const string MensajeNoCoinciden =
        "Ya no coinciden en ese puente: puede que alguno haya cambiado su horario o su test.";
    public const string MensajeYaRespondida = "Ya respondiste a este saludo.";

    private static readonly string[] Dias = ["", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado"];

    public static string Dia(int diaSemana) => diaSemana is >= 1 and <= 6 ? Dias[diaSemana] : "";

    /// <summary>Si ya hay una conexión con esa persona, en cualquier sentido, explica por qué no se puede saludar otra vez.</summary>
    public static string MensajeYaExiste(string estado, bool laEnvieYo) => (estado, laEnvieYo) switch
    {
        (Aceptada, _) => "Ya están conectados: busca su correo en «Tus conexiones».",
        (Pendiente, false) => "Esta persona ya te saludó: respóndele en «Te quieren conocer».",
        (Rechazada, false) => "Ya respondiste al saludo de esta persona.",
        _ => "Ya saludaste a esta persona.",
    };

    /// <summary>El aviso para quien recibe el saludo. Nunca pasa de 200 caracteres (la columna mensaje).</summary>
    public static string MensajeSolicitada(string nombreCorto, int compatibilidad, int diaSemana, TimeOnly inicio, TimeOnly fin) =>
        Recortar($"{nombreCorto} ({compatibilidad} % compatible) quiere coincidir contigo el {Dia(diaSemana)} " +
                 $"de {inicio:HH\\:mm} a {fin:HH\\:mm}. Respóndele en Libres contigo.");

    /// <summary>El aviso para quien saludó, cuando la otra persona acepta.</summary>
    public static string MensajeAceptada(string nombreCorto, int diaSemana, TimeOnly inicio, TimeOnly fin) =>
        Recortar($"{nombreCorto} aceptó tu saludo. Ya pueden ver su correo para verse el {Dia(diaSemana)} " +
                 $"de {inicio:HH\\:mm} a {fin:HH\\:mm}.");

    /// <summary>Solo quien recibió el saludo responde, y una sola vez.</summary>
    public static string? ErrorAlResponder(string estadoActual, bool soyDestinatario)
    {
        if (!soyDestinatario) return "NO_ENCONTRADO";
        return estadoActual == Pendiente ? null : ErrorYaRespondida;
    }

    private static string Recortar(string texto) => texto.Length <= 200 ? texto : texto[..199] + "…";
}
