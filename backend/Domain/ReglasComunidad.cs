namespace Breaku.Domain.Rules;

/// <summary>
/// Reglas puras de la creación automática de comunidades (HU13). No dependen de nada.
/// Una comunidad solo se crea si hay al menos un compañero libre a la hora de la actividad: si no, no tiene sentido
/// armar un grupo. Se invita a los más compatibles, uno por plaza libre, y cada uno recibe un aviso.
/// </summary>
public static class ReglasComunidad
{
    /// <summary>Con menos tiempo en común no da para hacer nada juntos (el mismo mínimo que el matching de HU12).</summary>
    public const int MinutosMinimosEnComun = ReglasMatching.MinutosMinimosEnComun;

    /// <summary>notificacion.mensaje es VARCHAR(200).</summary>
    public const int MaximoLargoMensaje = 200;

    public const string MensajeSinZona = "Elige tu campus para encontrar compañeros libres a esa hora.";
    public const string MensajeNoVisible = "Para invitar a tus compañeros, primero acepta que ellos también te encuentren.";
    public const string MensajeSinCoincidencias = "Nadie de tu campus está libre a esa hora. Prueba con otra hora o vuelve a intentarlo más tarde.";

    /// <summary>Día de la semana como en horario y puente: 1 = lunes … 7 = domingo.</summary>
    public static int DiaSemana(DateTime fecha) => fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)fecha.DayOfWeek;

    /// <summary>
    /// Minutos en común que se exigen. Una actividad corta (por ejemplo, 15 minutos) se puede compartir entera:
    /// basta con que el compañero esté libre todo ese rato.
    /// </summary>
    public static int MinutosExigidos(int duracionMin) => Math.Min(MinutosMinimosEnComun, duracionMin);

    /// <summary>Plazas para invitar: el grupo ya cuenta a quien lo crea.</summary>
    public static int Plazas(int maxParticipantes) => Math.Max(0, maxParticipantes - 1);

    /// <summary>Compañero elegido para la invitación y cuánto tiempo comparte con la actividad.</summary>
    public readonly record struct Invitado(int UsuarioId, ReglasMatching.Franja EnComun, int MinutosEnComun);

    /// <summary>
    /// Elige a quién invitar: compañeros con un puente el mismo día que se cruce con la actividad al menos
    /// <see cref="MinutosExigidos"/> minutos. Primero quien comparte más tiempo; en empate, por <c>UsuarioId</c>
    /// para que el resultado sea siempre el mismo. Uno por plaza libre.
    /// Si un compañero tiene dos puentes que tocan la actividad, cuenta el tramo más largo.
    /// </summary>
    public static List<Invitado> ElegirInvitados(ReglasMatching.Franja actividad, int maxParticipantes,
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
            .Take(Plazas(maxParticipantes))
            .ToList();
    }

    /// <summary>Hora local de Bolivia → UTC (Bolivia es UTC-4 todo el año).</summary>
    public static DateTime AUtc(DateTime local) => DateTime.SpecifyKind(local - ReglasNotificacion.DesfaseBolivia, DateTimeKind.Utc);

    /// <summary>Solo el primer nombre: en un aviso basta para saber quién invita.</summary>
    public static string PrimerNombre(string nombre)
    {
        var limpio = nombre.Trim();
        var espacio = limpio.IndexOf(' ');
        return espacio > 0 ? limpio[..espacio] : limpio;
    }

    /// <summary>
    /// Texto del aviso de invitación, de 200 caracteres como mucho. Si no entra, se acorta la actividad (nunca la hora).
    /// </summary>
    public static string MensajeInvitacion(string creador, string actividad, TimeOnly inicio, TimeOnly fin)
    {
        var quien = PrimerNombre(creador);
        string Armar(string a) => $"{quien} te invita a «{a}» hoy de {inicio:HH\\:mm} a {fin:HH\\:mm}. Coincide con tu tiempo libre.";

        var mensaje = Armar(actividad);
        if (mensaje.Length <= MaximoLargoMensaje) return mensaje;

        var sobra = mensaje.Length - MaximoLargoMensaje + 1;   // +1 por los puntos suspensivos
        var corta = actividad[..Math.Max(1, actividad.Length - sobra)].TrimEnd() + "…";
        mensaje = Armar(corta);
        return mensaje.Length <= MaximoLargoMensaje ? mensaje : mensaje[..MaximoLargoMensaje];
    }
}
