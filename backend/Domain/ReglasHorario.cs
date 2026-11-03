namespace Breaku.Domain.Rules;

public static class ReglasHorario
{
    public static readonly TimeOnly InicioMasTemprano = new(7, 15);

    public static readonly TimeOnly FinMasTarde = new(21, 0);

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
