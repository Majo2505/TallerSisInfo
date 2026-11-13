namespace Breaku.Domain.Rules;

public static class ReglasAsistencia
{
    public const string ErrorNoEncontrado = "NO_ENCONTRADO";
    public const string ErrorValidacion = "VALIDACION";
    public const string ErrorCompleta = "COMPLETA";
    public const string ErrorCruce = "CRUCE";

    public const string MensajeNoEncontrada = "La comunidad no existe o ya no está disponible.";
    public const string MensajeFaltaRespuesta = "Indica si vas o no.";
    public const string MensajeEresCreador = "Creaste esta comunidad: tú siempre vas.";
    /// <summary>La comunidad cambió de creador cuando el creador original fue dado de baja (HU28/HU29).</summary>
    public const string MensajeEresOrganizador = "Ahora organizas esta comunidad.";
    public const string MensajeTerminada = "La actividad ya terminó.";
    public const string MensajeNoEstabas = "No estás en esta comunidad.";
    public const string MensajeCompleta = "Ya no quedan lugares en esta comunidad.";

    public const int MaximoLargoAviso = 200;

    public readonly record struct Situacion(
        bool Existe,
        bool EsCreador,
        bool Terminada,
        string? MiEstado,
        bool PuedeVerla,
        int Confirmados,
        int Maximo,
        bool HayCruce,
        bool CreadorHeredado = false);

    public readonly record struct Decision(string? Error, string? Mensaje, string Estado, bool Cambia)
    {
        public static Decision Falla(string error, string mensaje) => new(error, mensaje, "", false);
        public static Decision Queda(string estado) => new(null, null, estado, false);
        public static Decision Pasa(string estado) => new(null, null, estado, true);
    }

    public static Decision Decidir(Situacion s, bool asisto)
    {
        if (!s.Existe || (!s.EsCreador && s.MiEstado is null && !s.PuedeVerla))
            return Decision.Falla(ErrorNoEncontrado, MensajeNoEncontrada);
        if (s.EsCreador)
            return Decision.Falla(ErrorValidacion, s.CreadorHeredado ? MensajeEresOrganizador : MensajeEresCreador);
        if (s.Terminada)
            return Decision.Falla(ErrorValidacion, MensajeTerminada);

        if (!asisto)
        {
            if (s.MiEstado is null) return Decision.Falla(ErrorValidacion, MensajeNoEstabas);
            return s.MiEstado == Participacion.EstadoRechazado
                ? Decision.Queda(Participacion.EstadoRechazado)
                : Decision.Pasa(Participacion.EstadoRechazado);
        }

        if (s.MiEstado == Participacion.EstadoConfirmado)
            return Decision.Queda(Participacion.EstadoConfirmado);
        if (s.Confirmados >= s.Maximo)
            return Decision.Falla(ErrorCompleta, MensajeCompleta);
        if (s.HayCruce)
            return Decision.Falla(ErrorCruce, "");
        return Decision.Pasa(Participacion.EstadoConfirmado);
    }

    public static int ConfirmadosDespues(int confirmados, string? antes, string despues)
    {
        var estabaConfirmado = antes == Participacion.EstadoConfirmado;
        var quedaConfirmado = despues == Participacion.EstadoConfirmado;
        if (estabaConfirmado == quedaConfirmado) return confirmados;
        return quedaConfirmado ? confirmados + 1 : confirmados - 1;
    }

    public static bool SeLlena(int confirmadosAntes, int confirmadosDespues, int maximo)
        => confirmadosAntes < maximo && confirmadosDespues >= maximo;

    public static bool SeReabre(int confirmadosAntes, int confirmadosDespues, int maximo)
        => confirmadosAntes >= maximo && confirmadosDespues < maximo;

    public static string MensajeCruce(string actividad, TimeOnly inicio, TimeOnly fin)
        => $"A esa hora ya vas a «{actividad}» ({inicio:HH\\:mm}–{fin:HH\\:mm}).";

    public static string MensajeConfirmado(string actividad, TimeOnly inicio, TimeOnly fin)
        => $"¡Listo! Te esperamos en «{actividad}» hoy de {inicio:HH\\:mm} a {fin:HH\\:mm}.";

    public const string MensajeRechazado = "Listo, quedó registrado que no vas.";

    public static string MensajeCerrada(string actividad)
    {
        string Armar(string a) => $"«{a}» ya se llenó. Si alguien se baja, todavía puedes confirmar desde Comunidades.";

        var mensaje = Armar(actividad);
        if (mensaje.Length <= MaximoLargoAviso) return mensaje;

        var sobra = mensaje.Length - MaximoLargoAviso + 1;
        var corta = actividad[..Math.Max(1, actividad.Length - sobra)].TrimEnd() + "…";
        return Armar(corta);
    }

    public static int Orden(string estado) => estado switch
    {
        Participacion.EstadoConfirmado => 0,
        Participacion.EstadoPendiente => 1,
        _ => 2
    };
}
