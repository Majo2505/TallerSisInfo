namespace Breaku.Domain.Rules;

public static class ReglasHorario
{
    public static readonly TimeOnly InicioMasTemprano = new(7, 15);

    public static readonly TimeOnly FinMasTarde = new(21, 0);

    public const int DuracionMinimaMin = 45;

    public const string MensajeDuracionMinima = "Una clase dura al menos 45 minutos.";

    /// <summary>La última hora a la que puede empezar una clase (20:15), para que dure 45 min antes de las 21:00.</summary>
    public static readonly TimeOnly InicioMasTarde = FinMasTarde.AddMinutes(-DuracionMinimaMin);

    /// <summary>La primera hora a la que puede terminar una clase (08:00), si empezó a las 07:15.</summary>
    public static readonly TimeOnly FinMasTemprano = InicioMasTemprano.AddMinutes(DuracionMinimaMin);

    public static string? ValidarBloque(string? materia, int diaSemana, TimeOnly inicio, TimeOnly fin)
    {
        if (string.IsNullOrWhiteSpace(materia)) return "La materia es obligatoria.";
        if (materia.Trim().Length > 100) return "La materia no puede superar 100 caracteres.";
        if (diaSemana < 1 || diaSemana > 6) return "El día debe ir de 1 (lunes) a 6 (sábado).";
        if (inicio < InicioMasTemprano) return "Las clases empiezan desde las 07:15.";
        if (fin > FinMasTarde) return "Las clases terminan, como muy tarde, a las 21:00.";
        if (fin <= inicio) return "La hora de fin tiene que ser posterior a la hora de inicio.";
        if ((fin - inicio).TotalMinutes < DuracionMinimaMin) return MensajeDuracionMinima;
        return null;
    }

    public static bool Solapa(TimeOnly aIni, TimeOnly aFin, TimeOnly bIni, TimeOnly bFin)
        => aIni < bFin && bIni < aFin;

    public readonly record struct Intervalo(TimeOnly Inicio, TimeOnly Fin);

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

    public static bool HayChoque(Horario a, Horario b)
        => a.Asiste && b.Asiste && a.DiaSemana == b.DiaSemana && Solapa(a.HoraInicio, a.HoraFin, b.HoraInicio, b.HoraFin);

    public static List<Horario> Coincidencias(Horario bloque, IEnumerable<Horario> otros)
        => otros.Where(o => !ReferenceEquals(o, bloque) && (bloque.Id == 0 || o.Id != bloque.Id)
                            && o.DiaSemana == bloque.DiaSemana
                            && Solapa(bloque.HoraInicio, bloque.HoraFin, o.HoraInicio, o.HoraFin)).ToList();

    public static List<Horario> Choques(Horario bloque, IEnumerable<Horario> otros)
        => bloque.Asiste ? Coincidencias(bloque, otros).Where(o => o.Asiste).ToList() : [];
}
