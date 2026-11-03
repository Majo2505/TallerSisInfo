using System.Globalization;
using System.Text.RegularExpressions;

namespace Breaku.Domain.Rules;

public sealed record ArgumentosExtraidos(
    bool Entendido,
    string? Actividad,
    string? Hora,
    int? EnMinutos,
    int? DuracionMin,
    int? MaxParticipantes
);

public sealed record DatosActividad(string? Actividad, TimeOnly? HoraInicio, int? DuracionMin, int? MaxParticipantes);

public sealed record ResultadoExtraccion(
    string Estado,
    DatosActividad Datos,
    IReadOnlyList<string> Faltantes,
    IReadOnlyList<string> Supuestos,
    string? Mensaje
);

public static class ReglasExtraccion
{
    public const string EstadoCompleto = "COMPLETO";
    public const string EstadoIncompleto = "INCOMPLETO";
    public const string EstadoNoEntendido = "NO_ENTENDIDO";

    public const string CampoActividad = "actividad";
    public const string CampoHoraInicio = "horaInicio";
    public const string CampoDuracion = "duracionMin";
    public const string CampoParticipantes = "maxParticipantes";

    public const int MaximoLargoTexto = 200;

    public const int MaximoLargoActividad = 100;

    public const int DuracionMinimaMin = 10;
    public const int DuracionMaximaMin = 240;

    public const int MinimoParticipantes = 2;
    public const int MaximoParticipantes = 30;

    public const int ToleranciaPasadoMin = 5;

    public const string MensajeNoEntendido = "No entendimos tu actividad, ¿puedes describirla con más detalle?";

    public const string PreguntaHora = "¿A qué hora quieres empezar?";
    public const string PreguntaHoraFueraDeRango = "Las actividades en el campus van de 07:15 a 21:00. ¿A qué hora quieres empezar?";
    public const string PreguntaHoraPasada = "Esa hora ya pasó y las actividades son para hoy. ¿A qué hora quieres empezar?";
    public const string PreguntaDuracion = "¿Cuánto tiempo va a durar la actividad?";
    public const string PreguntaDuracionFueraDeRango = "La actividad puede durar entre 10 minutos y 4 horas. ¿Cuánto tiempo va a durar?";
    public const string PreguntaDuracionPasaLasNueve = "Así terminaría después de las 21:00. ¿Cuánto tiempo va a durar?";
    public const string PreguntaParticipantes = "¿Cuántas personas pueden participar, contándote a ti?";
    public const string PreguntaParticipantesFueraDeRango = "El grupo puede ser de 2 a 30 personas, contándote a ti. ¿Cuántas personas pueden participar?";

    private static readonly string[] FormatosHora = ["HH:mm", "H:mm", "HH:mm:ss"];
    private static readonly char[] PuntuacionExtremos = ['.', ',', ';', ':', '!', '¡', '?', '¿', '"', '\'', '«', '»', '-'];

    public static bool EsDemasiadoCorto(string? texto)
        => string.IsNullOrWhiteSpace(texto) || ReglasSeguridadIa.Normalizar(texto).Count(char.IsAsciiLetter) < 3;

    public static TimeOnly? LeerHora(string? hora)
        => TimeOnly.TryParseExact(hora?.Trim(), FormatosHora, CultureInfo.InvariantCulture, DateTimeStyles.None, out var h) ? h : null;

    public static string? LimpiarActividad(string? actividad)
    {
        if (string.IsNullOrWhiteSpace(actividad)) return null;
        var limpia = ReglasSeguridadIa.Sanear(actividad).Trim(PuntuacionExtremos).Trim();
        limpia = Regex.Replace(limpia, @"\s+", " ");
        if (limpia.Count(char.IsLetter) < 2) return null;
        return limpia.Length > MaximoLargoActividad ? limpia[..MaximoLargoActividad].TrimEnd() : limpia;
    }

    public static ResultadoExtraccion Interpretar(ArgumentosExtraidos? nuevos, DatosActividad? anterior, DateTime ahoraLocal)
    {
        var actividad = LimpiarActividad(nuevos?.Actividad) ?? LimpiarActividad(anterior?.Actividad);
        var noEntendioNada = anterior is null && nuevos is { Entendido: false };
        if (actividad is null || noEntendioNada)
        {
            return new ResultadoExtraccion(EstadoNoEntendido, new DatosActividad(null, null, null, null),
                [CampoActividad], [], MensajeNoEntendido);
        }

        var faltantes = new List<string>();
        var supuestos = new List<string>();
        string? primeraPregunta = null;

        void Falta(string campo, string pregunta)
        {
            faltantes.Add(campo);
            primeraPregunta ??= pregunta;
        }

        var ahora = new TimeOnly(ahoraLocal.Hour, ahoraLocal.Minute);
        TimeOnly? hora = null;
        string? problemaHora = null;

        if (nuevos?.EnMinutos is int enMinutos)
        {
            var inicio = ahoraLocal.AddMinutes(enMinutos);
            if (enMinutos < 0 || inicio.Date != ahoraLocal.Date) problemaHora = PreguntaHoraFueraDeRango;
            else hora = new TimeOnly(inicio.Hour, inicio.Minute);
        }
        else if (LeerHora(nuevos?.Hora) is { } horaDicha)
        {
            hora = horaDicha;
        }
        else if (anterior?.HoraInicio is { } horaAnterior)
        {
            hora = horaAnterior;
        }
        else
        {
            hora = ahora;
            supuestos.Add(CampoHoraInicio);
        }

        if (hora is { } h)
        {
            if (h < ReglasHorario.InicioMasTemprano || h >= ReglasHorario.FinMasTarde) problemaHora = PreguntaHoraFueraDeRango;
            else if (Minutos(h) < Minutos(ahora) - ToleranciaPasadoMin) problemaHora = PreguntaHoraPasada;
        }

        if (problemaHora is not null)
        {
            hora = null;
            supuestos.Remove(CampoHoraInicio);
        }

        var duracion = nuevos?.DuracionMin ?? anterior?.DuracionMin;
        string? problemaDuracion = null;
        if (duracion is null) problemaDuracion = PreguntaDuracion;
        else if (duracion < DuracionMinimaMin || duracion > DuracionMaximaMin) problemaDuracion = PreguntaDuracionFueraDeRango;
        else if (hora is { } inicioValido && Minutos(inicioValido) + duracion > Minutos(ReglasHorario.FinMasTarde))
            problemaDuracion = PreguntaDuracionPasaLasNueve;
        if (problemaDuracion is not null) duracion = null;

        var participantes = nuevos?.MaxParticipantes ?? anterior?.MaxParticipantes;
        string? problemaParticipantes = null;
        if (participantes is null) problemaParticipantes = PreguntaParticipantes;
        else if (participantes < MinimoParticipantes || participantes > MaximoParticipantes) problemaParticipantes = PreguntaParticipantesFueraDeRango;
        if (problemaParticipantes is not null) participantes = null;

        if (problemaHora is not null) Falta(CampoHoraInicio, problemaHora);
        if (problemaDuracion is not null) Falta(CampoDuracion, problemaDuracion);
        if (problemaParticipantes is not null) Falta(CampoParticipantes, problemaParticipantes);

        var datos = new DatosActividad(actividad, hora, duracion, participantes);
        return faltantes.Count == 0
            ? new ResultadoExtraccion(EstadoCompleto, datos, [], supuestos, null)
            : new ResultadoExtraccion(EstadoIncompleto, datos, faltantes, supuestos, primeraPregunta);
    }

    private static int Minutos(TimeOnly t) => t.Hour * 60 + t.Minute;
}
