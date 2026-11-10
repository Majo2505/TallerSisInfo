namespace Breaku.Domain;

public static class ReglasEventos
{
    public static bool CabeEnPuente(DateTime inicioLocal, DateTime finLocal, IEnumerable<Puente> puentes)
    {
        if (finLocal <= inicioLocal || inicioLocal.Date != finLocal.Date) return false;

        var dia = (int)inicioLocal.DayOfWeek;
        var desde = TimeOnly.FromDateTime(inicioLocal);
        var hasta = TimeOnly.FromDateTime(finLocal);
        return puentes.Any(p => p.DiaSemana == dia && p.HoraInicio <= desde && p.HoraFin >= hasta);
    }
}
