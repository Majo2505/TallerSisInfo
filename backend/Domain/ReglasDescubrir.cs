namespace Breaku.Domain.Rules;

public static class ReglasDescubrir
{
    public const string EstadoOk = "OK";
    public const string EstadoSinResultados = "SIN_RESULTADOS";
    public const string EstadoSinZona = "SIN_ZONA";
    public const string EstadoNoVisible = "NO_VISIBLE";

    public const string MensajeSinResultados = "Hoy todavía no hay comunidades activas en tu campus. ¡Crea la primera!";
    public const string MensajeSinZona = "Elige tu campus para ver las comunidades de tus compañeros.";
    public const string MensajeNoVisible = "Para ver las comunidades de tus compañeros, primero acepta que ellos también te encuentren.";

    public const int Maximo = 50;

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

    public static int Confirmados(int confirmadosEnTabla, bool creadorEnTabla)
        => confirmadosEnTabla + (creadorEnTabla ? 0 : 1);

    public static (string Estado, string? Mensaje) Estado(bool tieneZona, bool visible, int cantidad)
    {
        if (!tieneZona) return (EstadoSinZona, MensajeSinZona);
        if (!visible) return (EstadoNoVisible, MensajeNoVisible);
        return cantidad == 0 ? (EstadoSinResultados, MensajeSinResultados) : (EstadoOk, null);
    }
}
