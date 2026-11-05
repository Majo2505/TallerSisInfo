using Breaku.Domain.Rules;

namespace Breaku.Domain;

public static class ReglasDisponibilidad
{
    public static readonly TimeOnly Apertura = new(7, 0);
    public static readonly TimeOnly Cierre = new(21, 0);

    public static bool EnHorarioOperativo(DateTime ahoraUtc)
    {
        var hora = TimeOnly.FromDateTime(ReglasNotificacion.AHoraLocal(ahoraUtc));
        return hora >= Apertura && hora < Cierre;
    }
}
