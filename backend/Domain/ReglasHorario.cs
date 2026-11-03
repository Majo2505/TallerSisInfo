namespace Breaku.Domain.Rules;

/// <summary>Reglas puras del horario. No dependen de nada.</summary>
public static class ReglasHorario
{
    /// <summary>La clase más temprana de la UCB empieza a esta hora (ck_horario_rango).</summary>
    public static readonly TimeOnly InicioMasTemprano = new(7, 15);

    /// <summary>La clase más tarde de la UCB termina a esta hora (ck_horario_rango).</summary>
    public static readonly TimeOnly FinMasTarde = new(21, 0);

    /// <summary>Devuelve el mensaje de error, o null si el bloque es válido.</summary>
    public static string? ValidarBloque(string? materia, int diaSemana, TimeOnly inicio, TimeOnly fin)
    {
        if (string.IsNullOrWhiteSpace(materia)) return "La materia es obligatoria.";
        if (materia.Trim().Length > 100) return "La materia no puede superar 100 caracteres.";
        if (diaSemana < 1 || diaSemana > 6) return "El día debe ir de 1 (lunes) a 6 (sábado).";
        if (inicio < InicioMasTemprano) return "Las clases empiezan desde las 07:15.";
        if (fin > FinMasTarde) return "Las clases terminan, como muy tarde, a las 21:00.";
        if (fin <= inicio) return "La hora de fin tiene que ser posterior a la hora de inicio.";
        return null;
    }

    /// <summary>Dos bloques se solapan si se cruzan; si uno termina justo cuando empieza el otro, NO.</summary>
    public static bool Solapa(TimeOnly aIni, TimeOnly aFin, TimeOnly bIni, TimeOnly bFin)
        => aIni < bFin && bIni < aFin;

    /// <summary>Un tramo de tiempo ocupado dentro de un día.</summary>
    public readonly record struct Intervalo(TimeOnly Inicio, TimeOnly Fin);

    /// <summary>
    /// Une los intervalos que se cruzan o se tocan en uno solo. El resultado queda ordenado, sin solapes
    /// y sin duplicados, así que de él se pueden sacar huecos sin riesgo de valores negativos.
    /// </summary>
    public static List<Intervalo> UnirIntervalos(IEnumerable<Intervalo> intervalos)
    {
        var unidos = new List<Intervalo>();
        foreach (var actual in intervalos.OrderBy(i => i.Inicio).ThenBy(i => i.Fin))
        {
            if (unidos.Count > 0 && actual.Inicio <= unidos[^1].Fin)
            {
                if (actual.Fin > unidos[^1].Fin) unidos[^1] = unidos[^1] with { Fin = actual.Fin };
            }
            else
            {
                unidos.Add(actual);
            }
        }
        return unidos;
    }

    /// <summary>
    /// Hay choque cuando dos bloques del mismo día se cruzan y el estudiante asiste a los dos.
    /// Un bloque marcado como «no asisto» nunca choca.
    /// </summary>
    public static bool HayChoque(Horario a, Horario b)
        => a.Asiste && b.Asiste && a.DiaSemana == b.DiaSemana && Solapa(a.HoraInicio, a.HoraFin, b.HoraInicio, b.HoraFin);

    /// <summary>Bloques del mismo día que se cruzan con <paramref name="bloque"/>, asista o no el estudiante (sirve para preguntarle a cuál asiste).</summary>
    public static List<Horario> Coincidencias(Horario bloque, IEnumerable<Horario> otros)
        => otros.Where(o => !ReferenceEquals(o, bloque) && (bloque.Id == 0 || o.Id != bloque.Id)
                            && o.DiaSemana == bloque.DiaSemana
                            && Solapa(bloque.HoraInicio, bloque.HoraFin, o.HoraInicio, o.HoraFin)).ToList();

    /// <summary>Los bloques a los que el estudiante asiste que chocan con <paramref name="bloque"/>. Vacío si él mismo no asiste.</summary>
    public static List<Horario> Choques(Horario bloque, IEnumerable<Horario> otros)
        => bloque.Asiste ? Coincidencias(bloque, otros).Where(o => o.Asiste).ToList() : [];
}
