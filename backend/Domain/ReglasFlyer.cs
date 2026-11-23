using System.Globalization;
using System.Text.RegularExpressions;

namespace Breaku.Domain.Rules;

/// <summary>Lo que el lector (LLM) dice haber visto en el flyer o en el texto. Todo es una sugerencia: nada se confía.</summary>
public sealed record EventoLeido(
    bool Entendido,
    bool Legible,
    string? Titulo,
    string? Descripcion,
    string? Inicio,
    string? Fin,
    string? Lugar,
    string? Asistencia,
    int? VisitaMinMin,
    int? VisitaMaxMin,
    bool VariosEventos,
    IReadOnlyList<string> Advertencias,
    // HU34: todas las fechas del MISMO evento que trae el afiche (conferencias, funciones). Inicio/Fin de arriba es la principal.
    IReadOnlyList<SesionLeida>? Sesiones = null
);

/// <summary>HU34: una fecha del evento tal como la leyó el modelo ("yyyy-MM-ddTHH:mm", hora de Bolivia). Nada se confía.</summary>
public sealed record SesionLeida(string? Inicio, string? Fin);

/// <summary>HU34: una fecha válida y futura del evento, en hora de Bolivia. Fin es null solo si no se pudo leer ni deducir.</summary>
public sealed record SesionEvento(DateTime Inicio, DateTime? Fin);

public sealed record BorradorEvento(
    string? Titulo,
    string? Descripcion,
    DateTime? Inicio,
    DateTime? Fin,
    string? LugarTexto,
    string? Asistencia,
    int? VisitaMin,
    int? VisitaMax
);

public sealed record ResultadoFlyer(
    string Estado,
    BorradorEvento Evento,
    IReadOnlyList<string> Faltantes,
    IReadOnlyList<string> Advertencias,
    string? Mensaje,
    // HU34: las fechas futuras del evento, ordenadas (máximo 5). La primera es la de Evento.Inicio/Fin.
    IReadOnlyList<SesionEvento>? Sesiones = null
);

/// <summary>HU24: convierte la lectura de un flyer en un borrador de evento. HU23 decide la publicación.</summary>
public static class ReglasFlyer
{
    public const string AsistenciaFija = "FIJA";
    public const string AsistenciaLibre = "LIBRE";

    public const string CampoTitulo = "titulo";
    public const string CampoInicio = "inicio";
    public const string CampoFin = "fin";
    public const string CampoLugar = "lugar";

    public const int MaximoLargoTitulo = 120;
    public const int MaximoLargoDescripcion = 1000;
    public const int MaximoLargoLugar = 150;
    public const int MaximoLargoTextoPegado = 4000;
    public const int MaximoAdvertencias = 5;
    public const int MaximoLargoAdvertencia = 160;

    /// <summary>Un evento de esta duración o más se sugiere LIBRE (se puede pasar un rato); menos, FIJO.</summary>
    public const int DuracionLibreMin = 180;
    public const int VisitaMinDefectoMin = 15;
    public const int VisitaMaxDefectoMin = 30;
    public const int VisitaMaximaPermitidaMin = 240;

    public const string MensajeNoEsEvento = "No encontramos un evento en lo que enviaste. Revisa el archivo o llena el formulario a mano.";
    public const string MensajeIlegible = "La imagen se ve borrosa; revisa los datos o súbela con más calidad";
    public const string MensajeFaltan = "No pudimos leer todos los datos del evento. Completa los que faltan.";
    public const string MensajeFechaPasada = "La fecha del evento ya pasó. Indica una fecha futura.";
    public const string AvisoVariosEventos = "El afiche tiene más de un evento; tomamos el principal.";
    public const string AvisoContenidoDescartado = "Se descartó un dato del afiche que no pudimos aceptar.";

    /// <summary>HU34: como máximo se devuelven 5 fechas por afiche (cada una se crea como un evento aparte).</summary>
    public const int MaximoSesiones = 5;
    /// <summary>HU34: cuántas fechas pasadas se nombran en el aviso (el resto se resume con «y N más»).</summary>
    public const int MaximoFechasPasadasEnAviso = 3;

    public static string AvisoFechasPasadas(IReadOnlyList<DateTime> pasadas)
    {
        var nombradas = string.Join(", ", pasadas.Take(MaximoFechasPasadasEnAviso).Select(FechaCorta));
        var resto = pasadas.Count - MaximoFechasPasadasEnAviso;
        var lista = resto > 0 ? $"{nombradas} y {resto} más" : nombradas;
        return pasadas.Count == 1
            ? $"Dejamos fuera una fecha que ya pasó: {lista}."
            : $"Dejamos fuera {pasadas.Count} fechas que ya pasaron: {lista}.";
    }

    public static string AvisoDemasiadasSesiones(int total) =>
        $"El afiche trae {total} fechas; tomamos las {MaximoSesiones} más próximas.";

    private static string FechaCorta(DateTime f) => f.ToString("dd/MM HH:mm", CultureInfo.InvariantCulture);

    private static readonly string[] FormatosFecha =
        ["yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss"];

    public static DateTime? LeerFecha(string? texto)
        => DateTime.TryParseExact(texto?.Trim(), FormatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var f) ? f : null;

    public static ResultadoFlyer Interpretar(EventoLeido? leido, DateTime ahoraLocal)
    {
        var vacio = new BorradorEvento(null, null, null, null, null, null, null, null);
        if (leido is null || (!leido.Entendido && leido.Legible))
            return new ResultadoFlyer(ReglasExtraccion.EstadoNoEntendido, vacio, [], [], MensajeNoEsEvento);

        var advertencias = new List<string>();
        var descartado = false;

        string? Limpio(string? valor, int maximo, int minimoLetras)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            if (ReglasSeguridadIa.EsManipulacion(valor)) { descartado = true; return null; }
            var t = Regex.Replace(ReglasSeguridadIa.Sanear(valor), @"\s+", " ").Trim();
            if (t.Count(char.IsLetterOrDigit) < minimoLetras) return null;
            return t.Length > maximo ? t[..maximo].TrimEnd() : t;
        }

        var titulo = Limpio(leido.Titulo, MaximoLargoTitulo, 2);
        var descripcion = Limpio(leido.Descripcion, MaximoLargoDescripcion, 1);
        var lugar = Limpio(leido.Lugar, MaximoLargoLugar, 2);

        // HU34: el evento conserva la primera fecha futura; las demás van en Sesiones.
        var (sesiones, pasadas, totalFuturas) = Sesiones(leido, ahoraLocal);
        var fechaPasada = pasadas.Count > 0;
        DateTime? inicio = null, fin;
        if (sesiones.Count > 0)
        {
            inicio = sesiones[0].Inicio;
            fin = sesiones[0].Fin;
        }
        else
        {
            // Sin un inicio válido y futuro: se conserva el fin leído si todavía sirve (como antes de HU34).
            fin = LeerFecha(leido.Fin);
            if (fin is { } f && f <= ahoraLocal) { fin = null; fechaPasada = true; }
        }

        var asistencia = ElegirAsistencia(leido.Asistencia, inicio, fin);
        int? visitaMin = null, visitaMax = null;
        if (asistencia == AsistenciaLibre) (visitaMin, visitaMax) = ElegirVisita(leido.VisitaMinMin, leido.VisitaMaxMin, inicio, fin);

        foreach (var a in leido.Advertencias.Take(MaximoAdvertencias))
        {
            if (string.IsNullOrWhiteSpace(a)) continue;
            if (ReglasSeguridadIa.EsManipulacion(a)) { descartado = true; continue; }
            var t = Regex.Replace(ReglasSeguridadIa.Sanear(a), @"\s+", " ").Trim();
            advertencias.Add(t.Length > MaximoLargoAdvertencia ? t[..MaximoLargoAdvertencia].TrimEnd() : t);
        }
        if (leido.VariosEventos) advertencias.Add(AvisoVariosEventos);
        if (pasadas.Count > 0) advertencias.Add(AvisoFechasPasadas(pasadas));
        if (totalFuturas > MaximoSesiones) advertencias.Add(AvisoDemasiadasSesiones(totalFuturas));
        if (descartado) advertencias.Add(AvisoContenidoDescartado);

        var faltantes = new List<string>();
        if (titulo is null) faltantes.Add(CampoTitulo);
        if (inicio is null) faltantes.Add(CampoInicio);
        if (fin is null) faltantes.Add(CampoFin);
        if (lugar is null) faltantes.Add(CampoLugar);

        var datos = new BorradorEvento(titulo, descripcion, inicio, fin, lugar, asistencia, visitaMin, visitaMax);
        if (!leido.Legible)
            return new ResultadoFlyer(ReglasExtraccion.EstadoIncompleto, datos, faltantes, advertencias, MensajeIlegible, sesiones);
        if (faltantes.Count == 0)
            return new ResultadoFlyer(ReglasExtraccion.EstadoCompleto, datos, [], advertencias, null, sesiones);

        var mensaje = fechaPasada && (faltantes.Contains(CampoInicio) || faltantes.Contains(CampoFin)) ? MensajeFechaPasada : MensajeFaltan;
        return new ResultadoFlyer(ReglasExtraccion.EstadoIncompleto, datos, faltantes, advertencias, mensaje, sesiones);
    }

    /// <summary>
    /// HU34: junta la fecha principal y las sesiones leídas, descarta las que no se pueden leer, une las repetidas
    /// (mismo inicio) y separa las que ya empezaron. Una fecha sin hora de fin toma la duración de otra fecha del mismo
    /// evento que sí la trae. Devuelve las futuras ordenadas (máximo 5), las pasadas y cuántas futuras había en total.
    /// </summary>
    public static (List<SesionEvento> Futuras, List<DateTime> Pasadas, int TotalFuturas) Sesiones(EventoLeido leido, DateTime ahoraLocal)
    {
        var leidas = new List<SesionLeida> { new(leido.Inicio, leido.Fin) };
        if (leido.Sesiones is { } extra) leidas.AddRange(extra.Where(s => s is not null));

        var porInicio = new SortedDictionary<DateTime, DateTime?>();
        foreach (var s in leidas)
        {
            if (LeerFecha(s.Inicio) is not { } inicio) continue;
            var fin = LeerFecha(s.Fin);
            if (fin is { } f && f <= inicio) fin = null;
            if (!porInicio.TryGetValue(inicio, out var anterior) || anterior is null) porInicio[inicio] = fin;
        }

        TimeSpan? duracion = porInicio.Where(p => p.Value is not null).Select(p => p.Value!.Value - p.Key).Cast<TimeSpan?>().FirstOrDefault();
        var pasadas = porInicio.Keys.Where(i => i < ahoraLocal).ToList();
        var futuras = porInicio.Where(p => p.Key >= ahoraLocal)
            .Select(p => new SesionEvento(p.Key, p.Value ?? p.Key + duracion))
            .ToList();
        return (futuras.Take(MaximoSesiones).ToList(), pasadas, futuras.Count);
    }

    /// <summary>El LLM sugiere; si no dice nada válido, la regla de respaldo: 3 h o más → LIBRE, si no FIJA.</summary>
    public static string ElegirAsistencia(string? sugerida, DateTime? inicio, DateTime? fin)
    {
        var s = sugerida?.Trim().ToUpperInvariant();
        if (s is AsistenciaFija or AsistenciaLibre) return s;
        return inicio is { } i && fin is { } f && (f - i).TotalMinutes >= DuracionLibreMin ? AsistenciaLibre : AsistenciaFija;
    }

    /// <summary>Visita corta sugerida. Valores inválidos del modelo se reemplazan por 15–30 min, acotados a la duración.</summary>
    public static (int Min, int Max) ElegirVisita(int? minimo, int? maximo, DateTime? inicio, DateTime? fin)
    {
        int? duracion = inicio is { } i && fin is { } f ? (int)(f - i).TotalMinutes : null;
        var tope = Math.Min(VisitaMaximaPermitidaMin, duracion ?? VisitaMaximaPermitidaMin);

        if (minimo is > 0 && maximo is > 0 && minimo <= maximo && maximo <= tope) return (minimo.Value, maximo.Value);

        var max = Math.Min(VisitaMaxDefectoMin, tope);
        var min = Math.Min(VisitaMinDefectoMin, max);
        return (Math.Max(min, 1), Math.Max(max, 1));
    }
}
