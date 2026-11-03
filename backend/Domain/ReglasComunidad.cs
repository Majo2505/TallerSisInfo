namespace Breaku.Domain.Rules;

public static class ReglasComunidad
{
    public const int MinutosMinimosEnComun = ReglasMatching.MinutosMinimosEnComun;

    public const int MaximoLargoMensaje = 200;

    public const string MensajeSinZona = "Elige tu campus para invitar a compañeros libres a esa hora.";
    public const string MensajeNoVisible = "Para invitar a tus compañeros, primero acepta que ellos también te encuentren.";
    public const string MensajeSinCoincidencias = "Por ahora nadie de tu campus está libre a esa hora. Puedes volver a buscar más tarde.";
    public const string MensajeCompleta = "El grupo ya está completo.";

    public static string MensajeInvitados(int cantidad) => cantidad == 1
        ? "Invitamos a 1 compañero libre a esa hora."
        : $"Invitamos a {cantidad} compañeros libres a esa hora.";

    public static int DiaSemana(DateTime fecha) => fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)fecha.DayOfWeek;

    public static int MinutosExigidos(int duracionMin) => Math.Min(MinutosMinimosEnComun, duracionMin);

    public readonly record struct Invitado(int UsuarioId, ReglasMatching.Franja EnComun, int MinutosEnComun);

    public static List<Invitado> ElegirInvitados(ReglasMatching.Franja actividad, int plazas,
        IEnumerable<ReglasMatching.PuenteDe> puentesDeOtros)
    {
        var minimo = MinutosExigidos(actividad.Minutos);

        return puentesDeOtros
            .GroupBy(p => p.UsuarioId)
            .Select(g => g
                .Select(p => ReglasMatching.Cruce(actividad, p.Franja, minimo))
                .Where(c => c is not null)
                .Select(c => c!.Value)
                .OrderByDescending(c => c.Minutos)
                .ThenBy(c => c.Inicio)
                .Select(c => (Invitado?)new Invitado(g.Key, c, c.Minutos))
                .FirstOrDefault())
            .Where(i => i is not null)
            .Select(i => i!.Value)
            .OrderByDescending(i => i.MinutosEnComun)
            .ThenBy(i => i.UsuarioId)
            .Take(Math.Max(0, plazas))
            .ToList();
    }

    public static string PrimerNombre(string nombre)
    {
        var limpio = nombre.Trim();
        var espacio = limpio.IndexOf(' ');
        return espacio > 0 ? limpio[..espacio] : limpio;
    }

    public static string MensajeInvitacion(string creador, string actividad, TimeOnly inicio, TimeOnly fin)
    {
        var quien = PrimerNombre(creador);
        string Armar(string a) => $"{quien} te invita a «{a}» hoy de {inicio:HH\\:mm} a {fin:HH\\:mm}. Coincide con tu tiempo libre.";

        var mensaje = Armar(actividad);
        if (mensaje.Length <= MaximoLargoMensaje) return mensaje;

        var sobra = mensaje.Length - MaximoLargoMensaje + 1;
        var corta = actividad[..Math.Max(1, actividad.Length - sobra)].TrimEnd() + "…";
        mensaje = Armar(corta);
        return mensaje.Length <= MaximoLargoMensaje ? mensaje : mensaje[..MaximoLargoMensaje];
    }
}
