using System.Text.RegularExpressions;

namespace Breaku.Domain.Rules;

/// <summary>Lo que el administrador carga al crear o editar un lugar del catálogo.</summary>
public sealed record DatosLugar(
    string? Nombre,
    string? Descripcion,
    int? CategoriaId,
    decimal? Latitud,
    decimal? Longitud,
    int? DuracionEstimadaMin,
    TimeOnly? HoraApertura,
    TimeOnly? HoraCierre,
    int? MinAPieDesdeBloqueA);

/// <summary>Un campus activo: el lugar tiene que quedar dentro de alguno.</summary>
public readonly record struct CampusRef(string Nombre, decimal Latitud, decimal Longitud, int RadioMaxM);

/// <summary>
/// Reglas puras del catálogo de lugares que administra el admin. Un lugar solo sirve si el estudiante puede llegar: por
/// eso tiene que estar dentro del radio de un campus activo (el mismo radio con el que se buscan lugares viables).
/// </summary>
public static class ReglasLugar
{
    public const int LargoMinimoNombre = 3;
    public const int LargoMaximoNombre = 100;      // lugar.nombre es VARCHAR(100)
    public const int LargoMaximoDescripcion = 500;
    public const int DuracionMinima = 1;
    public const int DuracionMaxima = 240;
    public const int MinutosAPieMaximos = 60;

    public const string MensajeNombre = "El nombre tiene que tener entre 3 y 100 caracteres.";
    public const string MensajeDescripcion = "La descripción puede tener hasta 500 caracteres.";
    public const string MensajeCategoria = "Elige una categoría.";
    public const string MensajeUbicacion = "Indica la latitud y la longitud del lugar.";
    public const string MensajeFueraDelCampus = "El lugar tiene que estar dentro de un campus activo.";
    public const string MensajeDuracion = "La duración estimada tiene que ser de 1 a 240 minutos.";
    public const string MensajeHorario = "Indica la apertura y el cierre, y que cierre después de abrir (o deja los dos vacíos).";
    public const string MensajeMinutosAPie = "Los minutos a pie tienen que ser de 0 a 60.";
    public const string MensajeNombreRepetido = "Ya hay un lugar con ese nombre.";
    public const string MensajeEnUso = "Este lugar ya se usó en recomendaciones, reportes o comunidades. Desactívalo en lugar de eliminarlo.";

    /// <summary>Sin espacios de más: «  Biblioteca   Central » → «Biblioteca Central».</summary>
    public static string LimpiarNombre(string? nombre)
        => Regex.Replace(nombre ?? "", @"\s+", " ", RegexOptions.None, TimeSpan.FromSeconds(1)).Trim();

    /// <summary>La descripción sin espacios en los extremos, o <c>null</c> si queda vacía.</summary>
    public static string? LimpiarDescripcion(string? descripcion)
        => string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();

    /// <summary>El primer problema de los datos, o <c>null</c> si se pueden guardar.</summary>
    public static string? Validar(DatosLugar d, IReadOnlyCollection<CampusRef> campusActivos)
    {
        var nombre = LimpiarNombre(d.Nombre);
        if (nombre.Length is < LargoMinimoNombre or > LargoMaximoNombre) return MensajeNombre;
        if (LimpiarDescripcion(d.Descripcion)?.Length > LargoMaximoDescripcion) return MensajeDescripcion;
        if (d.CategoriaId is null or <= 0) return MensajeCategoria;

        if (d.Latitud is not decimal lat || d.Longitud is not decimal lng || lat is < -90 or > 90 || lng is < -180 or > 180)
            return MensajeUbicacion;
        if (!DentroDeAlgunCampus(lat, lng, campusActivos)) return MensajeFueraDelCampus;

        if (d.DuracionEstimadaMin is not int duracion || duracion is < DuracionMinima or > DuracionMaxima) return MensajeDuracion;
        if ((d.HoraApertura is null) != (d.HoraCierre is null)) return MensajeHorario;
        if (d.HoraApertura is { } abre && d.HoraCierre is { } cierra && cierra <= abre) return MensajeHorario;
        if (d.MinAPieDesdeBloqueA is < 0 or > MinutosAPieMaximos) return MensajeMinutosAPie;
        return null;
    }

    /// <summary>true si el punto queda dentro del radio de algún campus activo.</summary>
    public static bool DentroDeAlgunCampus(decimal latitud, decimal longitud, IEnumerable<CampusRef> campusActivos)
        => campusActivos.Any(c => ReglasBusquedaLugares.DistanciaMetros(c.Latitud, c.Longitud, latitud, longitud) <= c.RadioMaxM);
}
