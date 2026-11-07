namespace Breaku.Domain.Rules;

/// <summary>Reglas de la foto de un lugar del catálogo (admin).</summary>
public static class ReglasFotoLugar
{
    /// <summary>2 MB: el panel achica la foto antes de subirla, así que una foto real queda muy por debajo.</summary>
    public const int BytesMaximos = 2 * 1024 * 1024;

    public const string MensajeVacia = "Elige una foto para el lugar.";
    public const string MensajeGrande = "La foto no puede superar los 2 MB.";
    public const string MensajeTipo = "La foto tiene que ser JPG, PNG o WebP.";
    public const string MensajeSinFoto = "Este lugar no tiene foto.";

    public const string Jpeg = "image/jpeg";
    public const string Png = "image/png";
    public const string Webp = "image/webp";

    /// <summary>
    /// El tipo real de la imagen según sus primeros bytes. No se confía en la extensión ni en el tipo que manda el
    /// navegador: un archivo que no empieza como JPG, PNG o WebP no se guarda.
    /// </summary>
    public static string? TipoReal(ReadOnlySpan<byte> contenido)
    {
        if (contenido.Length >= 3 && contenido[0] == 0xFF && contenido[1] == 0xD8 && contenido[2] == 0xFF) return Jpeg;
        if (contenido.Length >= 8 && contenido[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return Png;
        if (contenido.Length >= 12 && contenido[..4].SequenceEqual("RIFF"u8) && contenido[8..12].SequenceEqual("WEBP"u8)) return Webp;
        return null;
    }

    /// <summary>Devuelve el tipo de la foto o el mensaje de por qué no se puede guardar.</summary>
    public static (string? Tipo, string? Error) Validar(byte[]? contenido)
    {
        if (contenido is null || contenido.Length == 0) return (null, MensajeVacia);
        if (contenido.Length > BytesMaximos) return (null, MensajeGrande);
        var tipo = TipoReal(contenido);
        return tipo is null ? (null, MensajeTipo) : (tipo, null);
    }

    /// <summary>
    /// La hora en que cambió la foto, redondeada al microsegundo (lo que guarda DATETIME(6)), para que la versión de la URL
    /// sea la misma al subirla y al leerla después de la base.
    /// </summary>
    public static DateTime Version(DateTime ahoraUtc) => new(ahoraUtc.Ticks - ahoraUtc.Ticks % 10, DateTimeKind.Utc);

    /// <summary>
    /// Ruta de la foto, con la versión para que el navegador la guarde y solo la vuelva a pedir cuando cambia.
    /// Sin foto: null, y el frontend muestra la ilustración de la categoría.
    /// </summary>
    public static string? Url(int lugarId, DateTime? actualizada)
        => actualizada is { } a ? $"/api/lugares/{lugarId}/foto?v={a.Ticks}" : null;
}
