using System.Globalization;
using System.Text;
using Breaku.Domain.Rules;

namespace Breaku.Domain;

public static class ReglasEvento
{
    public const int MaximoEventosPorDia = 3;
    public const int MaximoTitulo = 120;
    public const int MaximoDescripcion = 4_000;
    public const int MaximoLugar = 150;
    public const int MaximoVisitaMinutos = 825;
    public const int BytesMaximosImagen = 2 * 1024 * 1024;
    public const string AsistenciaFija = "FIJA";
    public const string AsistenciaLibre = "LIBRE";
    public const string MensajeContenidoPendiente = "El contenido requiere revisión de administración antes de publicarse.";
    public const string MensajeLugarPendiente = "El lugar no figura en el catálogo y requiere revisión de administración.";
    public const string MensajeFechaPendiente = "La fecha de inicio ya pasó; el evento requiere revisión de administración.";
    public const string MensajeHorarioPendiente = "El horario está fuera del horario del campus (07:15 a 21:00) y requiere revisión.";
    public const string MensajeLimiteDiario = "Ya alcanzaste el máximo de 3 eventos creados por día.";
    public const string MensajeInicioPasado = "No se puede editar ni cancelar un evento que ya empezó.";
    public const string MensajeEventoNoPendiente = "El evento ya fue revisado.";
    public const string MensajeFotoNuevaYQuitar = "No puedes subir una foto nueva y quitar la actual a la vez.";
    public const string MensajeOrigenInvalido = "El origen del evento no es válido.";
    public const string MensajeEventoNoEditable = "Solo se pueden editar los eventos pendientes o publicados.";
    public const string RangoHoy = "HOY";
    public const string RangoSemana = "SEMANA";
    public const string MensajeRangoInvalido = "El rango debe ser hoy o semana.";

    public const string MensajeEventoFinalizado = "El evento ya terminó, así que no se puede revisar.";
    public const string MensajeFinCorregidoPasado = "Con esos cambios el evento ya habría terminado: corrige la fecha de fin para poder aprobarlo.";

    /// <summary>HU37: al editar y aprobar, el fin que deja el admin tiene que seguir por delante (el evento no puede quedar ya terminado).</summary>
    public static bool FinCorregidoVigente(DateTime finUtc, DateTime ahoraUtc) => finUtc > ahoraUtc;

    // HU26: moderación de eventos por el admin.
    public const int MaximoMotivo = 500;
    /// <summary>Cuántos eventos rechazados muestra el historial del admin.</summary>
    public const int MaximoHistorialRechazados = 100;
    public const int MaximoAviso = 200;
    public const string MensajeEventoNoPublicado = "El evento ya no está publicado.";
    public const string CodigoReporteNoCorresponde = "REPORTE_NO_CORRESPONDE";
    public const string MensajeReporteNoCorresponde = "El reporte no existe o no es sobre este evento.";
    public const string MensajeReporteYaRevisado = "Ese reporte ya fue revisado.";
    public const string MensajeFaltaMotivoRechazo = "Escribe el motivo del rechazo.";
    public const string MensajeFaltaMotivoBaja = "Escribe el motivo para bajar el evento.";
    public const string MensajeMotivoLargo = "El motivo no puede superar 500 caracteres.";
    public const string MensajeCambiosSoloAlAprobar = "Solo se puede editar un evento al aprobarlo.";

    // HU33: detección de posibles duplicados, sin IA y sin tocar el esquema.
    public const string CodigoEventoDuplicado = "EVENTO_DUPLICADO";
    public const double SimilitudMinimaTitulo = 0.7;
    public const int ToleranciaInicioHoras = 3;
    public const int MargenBusquedaDuplicadosDias = 2;

    private static readonly HashSet<string> PalabrasVacias = new(StringComparer.Ordinal)
    {
        "a", "al", "ante", "con", "como", "de", "del", "desde", "donde", "durante", "e", "el", "ella", "en",
        "entre", "era", "es", "esa", "ese", "eso", "estas", "este", "esto", "estos", "la", "las", "le",
        "lo", "los", "me", "mi", "mis", "muy", "nuestra", "nuestro", "nuestras", "nuestros", "o", "para",
        "por", "que", "se", "sin", "sobre", "su", "sus", "tu", "tus", "un", "una", "unas", "unos", "y"
    };

    /// <summary>Un evento PUBLICADO o PENDIENTE con el que se compara uno nuevo. Nunca RECHAZADO ni CANCELADO.</summary>
    public sealed record CandidatoDuplicado(
        int Id,
        int CreadorId,
        string Titulo,
        DateTime InicioUtc,
        DateTime FinUtc,
        int? LugarId,
        string? LugarTexto,
        string? NombreLugar,
        string Estado);

    /// <summary>Minusculas, sin tildes ni signos y con los espacios unificados.</summary>
    public static string NormalizarTexto(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return "";
        var sinTildes = texto.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray();
        var limpio = new string(sinTildes).Normalize(NormalizationForm.FormC)
            .Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ')
            .ToArray();
        return string.Join(' ', new string(limpio).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Como <see cref="NormalizarTexto"/> pero sin palabras vacías: es lo que compara el título.</summary>
    public static string NormalizarTitulo(string? titulo)
        => string.Join(' ', NormalizarTexto(titulo)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(palabra => !PalabrasVacias.Contains(palabra)));

    /// <summary>Similitud de Jaccard entre las palabras del título normalizado (0 a 1).</summary>
    public static double SimilitudTitulos(string? primero, string? segundo)
    {
        var a = NormalizarTitulo(primero);
        var b = NormalizarTitulo(segundo);
        if (a == b) return a.Length == 0 ? 0 : 1;

        var palabrasA = new HashSet<string>(a.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var palabrasB = new HashSet<string>(b.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var todas = new HashSet<string>(palabrasA);
        todas.UnionWith(palabrasB);
        return todas.Count == 0 ? 0 : (double)palabrasA.Intersect(palabrasB).Count() / todas.Count;
    }

    /// <summary>
    /// Un candidato cuenta como duplicado solo si coincide el título, el lugar y el momento. Se ignora el propio id al editar.
    /// </summary>
    public static bool EsDuplicado(
        CandidatoDuplicado candidato,
        string titulo,
        int? lugarId,
        string lugarTexto,
        DateTimeOffset inicio,
        DateTimeOffset fin,
        int? idActual = null)
    {
        if (candidato.Estado is not (Evento.EstadoPublicado or Evento.EstadoPendiente)) return false;
        if (idActual is int propio && candidato.Id == propio) return false;
        if (SimilitudTitulos(candidato.Titulo, titulo) < SimilitudMinimaTitulo) return false;
        if (!MismoLugar(candidato, lugarId, lugarTexto)) return false;

        var inicioCandidato = new DateTimeOffset(DateTime.SpecifyKind(candidato.InicioUtc, DateTimeKind.Utc));
        var finCandidato = new DateTimeOffset(DateTime.SpecifyKind(candidato.FinUtc, DateTimeKind.Utc));
        var mismoDia = inicio.ToOffset(DesfaseBolivia).Date == inicioCandidato.ToOffset(DesfaseBolivia).Date;
        if (mismoDia && Math.Abs((inicio - inicioCandidato).TotalHours) <= ToleranciaInicioHoras) return true;

        return inicio < finCandidato && inicioCandidato < fin;
    }

    /// <summary>
    /// De todos los candidatos que coinciden, elige el que decide qué pasa: primero los del mismo creador (reenvío: 409) y,
    /// si no hay propios, los de otras personas (queda PENDIENTE). En cada grupo gana el de inicio más cercano; si empatan, el
    /// de menor id.
    /// </summary>
    public static CandidatoDuplicado? ElegirDuplicado(IEnumerable<CandidatoDuplicado> coincidentes, int creadorId, DateTimeOffset inicio)
    {
        var lista = coincidentes.ToList();
        var propios = lista.Where(c => c.CreadorId == creadorId).ToList();
        return (propios.Count > 0 ? propios : lista)
            .OrderBy(c => Math.Abs((inicio - new DateTimeOffset(DateTime.SpecifyKind(c.InicioUtc, DateTimeKind.Utc))).TotalMinutes))
            .ThenBy(c => c.Id)
            .FirstOrDefault();
    }

    private static bool MismoLugar(CandidatoDuplicado candidato, int? lugarId, string lugarTexto)
    {
        if (lugarId is int id && candidato.LugarId == id) return true;

        if (lugarId is int && candidato.LugarId is int) return false;

        var textoNuevo = NormalizarTexto(lugarTexto);
        var textoCandidato = NormalizarTexto(candidato.LugarTexto ?? candidato.NombreLugar);
        return textoNuevo.Length > 0 && textoNuevo == textoCandidato;
    }

    public static string MensajeEventoDuplicado(string titulo, int id)
        => $"Ya existe un evento muy parecido: «{titulo}» (evento #{id}). Revisa el título, la fecha y el lugar antes de volver a enviarlo.";

    public static string MotivoPosibleDuplicado(string titulo, DateTimeOffset inicio)
        => $"Posible duplicado de «{titulo}» ({inicio.ToOffset(DesfaseBolivia):dd/MM/yyyy}).";

    public static string AvisoDuplicadoAdmin(string titulo, int id)
        => $"Se publicó igual: coincide con «{titulo}» (evento #{id}). Revisa ambos eventos.";

    public static string UnirMotivos(params string?[] motivos)
    {
        var texto = string.Join(' ', motivos.Where(m => !string.IsNullOrWhiteSpace(m)));
        return texto.Length <= MaximoMotivo ? texto : texto[..(MaximoMotivo - 1)] + "…";
    }

    /// <summary>
    /// Texto del aviso al autor cuando el admin aprueba, rechaza o baja su evento. Incluye el motivo y cabe en
    /// <see cref="MaximoAviso"/> caracteres: si no entra, se recorta primero el motivo y después el título, con «…».
    /// </summary>
    /// <summary>HU38: lo que queda en el reporte (y ve quien reportó en «Mis reportes») cuando se baja el evento desde ahí.</summary>
    public static string RespuestaReporteBaja(string motivo)
    {
        var texto = $"Bajamos el evento. Motivo: {motivo.Trim()}";
        return texto.Length <= MaximoMotivo ? texto : texto[..(MaximoMotivo - 1)].TrimEnd() + "…";
    }

    public static string AvisoModeracion(string tipo, string titulo, string? motivo)
    {
        string Armar(string t, string? m) => tipo switch
        {
            Notificacion.TipoEventoAprobado => $"Tu evento «{t}» fue aprobado y ya está publicado.",
            Notificacion.TipoEventoBajado => $"Tu evento «{t}» fue retirado del apartado de eventos. Motivo: {m}",
            _ => $"Tu evento «{t}» fue rechazado. Motivo: {m}"
        };

        titulo = titulo.Trim();
        motivo = motivo?.Trim();
        var texto = Armar(titulo, motivo);
        if (texto.Length <= MaximoAviso) return texto;

        if (!string.IsNullOrEmpty(motivo))
        {
            var sobra = texto.Length - MaximoAviso + 1; // +1 por «…»
            if (motivo.Length - sobra >= 20)
                return Armar(titulo, motivo[..(motivo.Length - sobra)].TrimEnd() + "…");
            motivo = motivo[..Math.Min(motivo.Length, 19)].TrimEnd() + "…";
            texto = Armar(titulo, motivo);
            if (texto.Length <= MaximoAviso) return texto;
        }

        var sobraTitulo = texto.Length - MaximoAviso + 1;
        return Armar(titulo[..Math.Max(1, titulo.Length - sobraTitulo)].TrimEnd() + "…", motivo);
    }

    private static readonly TimeSpan DesfaseBolivia = TimeSpan.FromHours(-4);

    public static string? Validar(
        string? titulo,
        string? descripcion,
        string? lugarTexto,
        int? lugarId,
        DateTimeOffset? inicio,
        DateTimeOffset? fin,
        string? asistencia,
        int? visitaMin,
        int? visitaMax)
    {
        if (string.IsNullOrWhiteSpace(titulo)) return "El título es obligatorio.";
        if (titulo.Trim().Length > MaximoTitulo) return $"El título no puede superar {MaximoTitulo} caracteres.";
        if (descripcion?.Length > MaximoDescripcion) return $"La descripción no puede superar {MaximoDescripcion} caracteres.";
        if (lugarId is <= 0) return "Selecciona un lugar válido del catálogo.";
        if (string.IsNullOrWhiteSpace(lugarTexto) && lugarId is null) return "Selecciona un lugar o escribe dónde será el evento.";
        if (lugarTexto?.Trim().Length > MaximoLugar) return $"El lugar no puede superar {MaximoLugar} caracteres.";
        if (inicio is null) return "La fecha y hora de inicio son obligatorias.";
        if (fin is null) return "La fecha y hora de fin son obligatorias.";
        if (fin <= inicio) return "La fecha y hora de fin deben ser posteriores al inicio.";
        if (asistencia is not (AsistenciaFija or AsistenciaLibre)) return "Indica si hay que llegar a la hora o si la visita es libre.";

        if (asistencia == AsistenciaFija && (visitaMin is not null || visitaMax is not null))
            return "Los tiempos de visita solo se usan cuando la asistencia es libre.";

        if (asistencia == AsistenciaLibre)
        {
            if (visitaMin is null || visitaMax is null)
                return "Indica el tiempo mínimo y máximo de visita.";
            if (visitaMin < 1 || visitaMax < visitaMin || visitaMax > MaximoVisitaMinutos)
                return $"El tiempo de visita debe estar entre 1 y {MaximoVisitaMinutos} minutos y el máximo no puede ser menor al mínimo.";
        }

        return null;
    }

    /// <summary>
    /// Origen con el que se guarda un evento nuevo. El cliente solo puede pedir FORMULARIO o FLYER (vacío = FORMULARIO);
    /// cualquier otro valor, incluido ADMIN, es un error. Un administrador siempre crea con origen ADMIN.
    /// </summary>
    public static (string? Origen, string? Error) ResolverOrigen(bool esAdmin, string? solicitado)
    {
        var pedido = string.IsNullOrWhiteSpace(solicitado) ? Evento.OrigenFormulario : solicitado.Trim().ToUpperInvariant();
        if (pedido is not (Evento.OrigenFormulario or Evento.OrigenFlyer))
            return (null, MensajeOrigenInvalido);
        return (esAdmin ? Evento.OrigenAdmin : pedido, null);
    }

    public static ResultadoPublicacion Evaluar(
        bool esAdmin,
        bool lugarCatalogado,
        string titulo,
        string? descripcion,
        string? lugarTexto,
        DateTimeOffset inicio,
        DateTimeOffset fin,
        DateTimeOffset ahora)
    {
        if (esAdmin) return new ResultadoPublicacion(Evento.EstadoPublicado, null);

        var motivos = new List<string>();
        if (inicio <= ahora) motivos.Add(MensajeFechaPendiente);

        var inicioLocal = inicio.ToOffset(DesfaseBolivia);
        var finLocal = fin.ToOffset(DesfaseBolivia);
        if (inicioLocal.Date != finLocal.Date
            || inicioLocal.TimeOfDay < ReglasHorario.InicioMasTemprano.ToTimeSpan()
            || finLocal.TimeOfDay > ReglasHorario.FinMasTarde.ToTimeSpan())
            motivos.Add(MensajeHorarioPendiente);

        if (!lugarCatalogado) motivos.Add(MensajeLugarPendiente);
        var textoCompleto = string.Join(" ", new[] { titulo, descripcion, lugarTexto }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (ReglasSeguridadIa.Evaluar(textoCompleto) is not null)
            motivos.Add(MensajeContenidoPendiente);

        return motivos.Count == 0
            ? new ResultadoPublicacion(Evento.EstadoPublicado, null)
            : new ResultadoPublicacion(Evento.EstadoPendiente, string.Join(" ", motivos));
    }

    public static (string? TipoMime, string? Error) ValidarImagen(ReadOnlySpan<byte> contenido)
    {
        if (contenido.Length == 0) return (null, "La foto seleccionada está vacía.");
        if (contenido.Length > BytesMaximosImagen) return (null, "La foto no puede superar los 2 MB.");

        if (contenido.Length >= 3 && contenido[0] == 0xFF && contenido[1] == 0xD8 && contenido[2] == 0xFF)
            return ("image/jpeg", null);
        if (contenido.Length >= 8 && contenido[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return ("image/png", null);

        return (null, "La foto del evento debe ser JPG o PNG.");
    }

    public static DateTime VersionImagen(DateTime ahoraUtc)
        => new(ahoraUtc.Ticks - ahoraUtc.Ticks % 10, DateTimeKind.Utc);

    public static string? UrlImagen(int eventoId, DateTime? actualizadaEn)
        => actualizadaEn is { } fecha ? $"/api/eventos/{eventoId}/foto?v={fecha.Ticks}" : null;

    public static (DateTime DesdeUtc, DateTime HastaUtc) RangoDiaLocal(DateTimeOffset ahora)
    {
        var local = ahora.ToOffset(DesfaseBolivia);
        var inicioLocal = new DateTimeOffset(local.Year, local.Month, local.Day, 0, 0, 0, DesfaseBolivia);
        return (inicioLocal.UtcDateTime, inicioLocal.AddDays(1).UtcDateTime);
    }

    /// <summary>
    /// Fin (UTC) del rango pedido en hora local de Bolivia: "hoy" llega hasta las 00:00 de manana y "semana"
    /// son hoy y los 6 dias siguientes. Devuelve null si el rango no es valido.
    /// </summary>
    public static DateTime? HastaDelRango(string? rango, DateTimeOffset ahora)
    {
        var (_, finDia) = RangoDiaLocal(ahora);
        return rango?.Trim().ToUpperInvariant() switch
        {
            RangoHoy => finDia,
            RangoSemana => finDia.AddDays(6),
            _ => null
        };
    }

    public sealed record ResultadoPublicacion(string Estado, string? Motivo);
}
