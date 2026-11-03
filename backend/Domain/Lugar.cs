namespace Breaku.Domain;

/// <summary>
/// Lugar del catálogo (tabla lugar). Solo se lee: el alta y la aprobación las hace el administrador.
/// Solo los lugares en estado <c>ACTIVO</c> (verificados y aprobados) pueden mostrarse a los estudiantes.
/// </summary>
public class Lugar
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }

    /// <summary>Minutos que lleva hacer la actividad en el lugar, sin contar el traslado.</summary>
    public short DuracionEstimadaMin { get; set; }

    /// <summary><c>ACTIVO</c>, <c>EN_REVISION</c> o <c>INACTIVO</c>.</summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>Tipo de lugar según el relevamiento (<c>comida</c>, <c>aire_libre</c>, <c>campus</c>, <c>referencia</c>…). Puede ser <c>null</c>.</summary>
    public string? TipoLugar { get; set; }

    /// <summary>Modalidades separadas por coma (<c>solitario</c>, <c>grupal</c>). Puede ser <c>null</c>.</summary>
    public string? Modalidad { get; set; }

    /// <summary>Niveles de energía que pide el lugar, separados por coma (<c>bajo</c>, <c>medio</c>, <c>alto</c>). Puede ser <c>null</c>.</summary>
    public string? NivelEnergia { get; set; }

    /// <summary>Rango de tiempo que suele llevar el lugar, en minutos. Puede ser <c>null</c>.</summary>
    public short? DuracionMin { get; set; }
    public short? DuracionMax { get; set; }

    /// <summary>Actividades que se pueden hacer, como una lista JSON de textos. Puede ser <c>null</c>.</summary>
    public string? ActividadesSugeridas { get; set; }

    /// <summary>Minutos a pie desde el bloque A del campus, según el relevamiento. Puede ser <c>null</c>.</summary>
    public short? MinAPieDesdeBloqueA { get; set; }

    /// <summary>
    /// <c>false</c> para puntos de referencia (campus, parqueo, bloques): se guardan en el catálogo pero nunca se recomiendan.
    /// </summary>
    public bool Recomendable { get; set; } = true;

    /// <summary><c>false</c> mientras el equipo no revise los datos del lugar. Se muestra a los estudiantes como «por validar».</summary>
    public bool Validado { get; set; } = true;
}
