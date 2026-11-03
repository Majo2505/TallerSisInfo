using System.Globalization;
using System.Text.RegularExpressions;

namespace Breaku.Domain.Rules;

/// <summary>
/// Lo que devolvió el extractor (LLM o simulado) al llamar a la función <c>registrar_actividad</c>, tal cual y SIN validar.
/// </summary>
/// <param name="Entendido">false si el texto no describe ninguna actividad.</param>
/// <param name="Actividad">Qué se va a hacer, en pocas palabras.</param>
/// <param name="Hora">Hora de inicio concreta en formato 24 h (HH:mm).</param>
/// <param name="EnMinutos">Inicio relativo: minutos desde ahora («en 20 minutos» = 20, «ahora» = 0).</param>
/// <param name="DuracionMin">Duración en minutos.</param>
/// <param name="MaxParticipantes">Personas en total, contando a quien escribe.</param>
public sealed record ArgumentosExtraidos(
    bool Entendido,
    string? Actividad,
    string? Hora,
    int? EnMinutos,
    int? DuracionMin,
    int? MaxParticipantes
);

/// <summary>Datos de la actividad ya validados. Un campo en <c>null</c> es un dato que falta.</summary>
public sealed record DatosActividad(string? Actividad, TimeOnly? HoraInicio, int? DuracionMin, int? MaxParticipantes);

/// <summary>Resultado de interpretar una extracción: qué se entendió, qué falta y qué preguntarle al estudiante.</summary>
/// <param name="Estado"><c>COMPLETO</c>, <c>INCOMPLETO</c> o <c>NO_ENTENDIDO</c>.</param>
/// <param name="Datos">Datos válidos; los que faltan quedan en <c>null</c>.</param>
/// <param name="Faltantes">Campos que faltan o no son válidos, en el orden en que se preguntan.</param>
/// <param name="Supuestos">Campos que completó el sistema porque el estudiante no los dijo (hoy solo la hora: «ahora»).</param>
/// <param name="Mensaje">Pregunta por el primer dato faltante, o el mensaje de que no se entendió. <c>null</c> si está completo.</param>
public sealed record ResultadoExtraccion(
    string Estado,
    DatosActividad Datos,
    IReadOnlyList<string> Faltantes,
    IReadOnlyList<string> Supuestos,
    string? Mensaje
);

/// <summary>
/// Reglas puras de la extracción estructurada de actividades (HU11). El extractor solo propone datos:
/// qué es válido, qué falta y qué se le pregunta al estudiante lo decide siempre este código, nunca el LLM.
/// </summary>
public static class ReglasExtraccion
{
    public const string EstadoCompleto = "COMPLETO";
    public const string EstadoIncompleto = "INCOMPLETO";
    public const string EstadoNoEntendido = "NO_ENTENDIDO";

    // Nombres de los campos tal como viajan en el JSON de la API (faltantes y supuestos).
    public const string CampoActividad = "actividad";
    public const string CampoHoraInicio = "horaInicio";
    public const string CampoDuracion = "duracionMin";
    public const string CampoParticipantes = "maxParticipantes";

    /// <summary>Mismo largo que la solicitud libre del frontend (MAXIMO_SOLICITUD).</summary>
    public const int MaximoLargoTexto = 200;

    /// <summary>comunidad_efimera.actividad es VARCHAR(100).</summary>
    public const int MaximoLargoActividad = 100;

    public const int DuracionMinimaMin = 10;
    public const int DuracionMaximaMin = 240;

    /// <summary>ck_com_max: una comunidad necesita al menos 2 personas.</summary>
    public const int MinimoParticipantes = 2;
    public const int MaximoParticipantes = 30;

    /// <summary>Margen para una hora «de hace un rato» (el estudiante tardó en escribir): no se toma como pasada.</summary>
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

    /// <summary>Un texto con menos de 3 letras no describe ninguna actividad: no vale la pena llamar al LLM.</summary>
    public static bool EsDemasiadoCorto(string? texto)
        => string.IsNullOrWhiteSpace(texto) || ReglasSeguridadIa.Normalizar(texto).Count(char.IsAsciiLetter) < 3;

    /// <summary>Lee una hora HH:mm (o HH:mm:ss). null si no tiene ese formato.</summary>
    public static TimeOnly? LeerHora(string? hora)
        => TimeOnly.TryParseExact(hora?.Trim(), FormatosHora, CultureInfo.InvariantCulture, DateTimeStyles.None, out var h) ? h : null;

    /// <summary>
    /// Quita espacios repetidos, los signos &lt; &gt; y la puntuación de los extremos. Recorta a <see cref="MaximoLargoActividad"/>.
    /// null si no queda una actividad con al menos 2 letras.
    /// </summary>
    public static string? LimpiarActividad(string? actividad)
    {
        if (string.IsNullOrWhiteSpace(actividad)) return null;
        var limpia = ReglasSeguridadIa.Sanear(actividad).Trim(PuntuacionExtremos).Trim();
        limpia = Regex.Replace(limpia, @"\s+", " ");
        if (limpia.Count(char.IsLetter) < 2) return null;
        return limpia.Length > MaximoLargoActividad ? limpia[..MaximoLargoActividad].TrimEnd() : limpia;
    }

    /// <summary>
    /// Combina lo que propuso el extractor con los datos que ya se habían entendido antes (si el estudiante está
    /// respondiendo a una pregunta), valida cada campo y decide qué falta. Lo nuevo reemplaza a lo anterior;
    /// lo anterior también se vuelve a validar, porque llega del cliente.
    /// </summary>
    /// <param name="nuevos">Lo que propuso el extractor en esta consulta. null si no se llamó.</param>
    /// <param name="anterior">Lo que ya se había entendido en la pregunta anterior. null en la primera consulta.</param>
    /// <param name="ahoraLocal">Fecha y hora actual en Bolivia (las actividades son para hoy).</param>
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

        // ---------- hora de inicio ----------
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
            // Sin hora: la actividad es espontánea, empieza ahora. Se avisa como supuesto para que el estudiante lo vea.
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

        // ---------- duración ----------
        var duracion = nuevos?.DuracionMin ?? anterior?.DuracionMin;
        string? problemaDuracion = null;
        if (duracion is null) problemaDuracion = PreguntaDuracion;
        else if (duracion < DuracionMinimaMin || duracion > DuracionMaximaMin) problemaDuracion = PreguntaDuracionFueraDeRango;
        else if (hora is { } inicioValido && Minutos(inicioValido) + duracion > Minutos(ReglasHorario.FinMasTarde))
            problemaDuracion = PreguntaDuracionPasaLasNueve;
        if (problemaDuracion is not null) duracion = null;

        // ---------- participantes ----------
        var participantes = nuevos?.MaxParticipantes ?? anterior?.MaxParticipantes;
        string? problemaParticipantes = null;
        if (participantes is null) problemaParticipantes = PreguntaParticipantes;
        else if (participantes < MinimoParticipantes || participantes > MaximoParticipantes) problemaParticipantes = PreguntaParticipantesFueraDeRango;
        if (problemaParticipantes is not null) participantes = null;

        // Se pregunta de a un dato, en este orden (HU10: preguntar específicamente por el dato faltante).
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
