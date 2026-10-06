namespace Breaku.Domain.Rules;

/// <summary>Reglas puras del horario. No dependen de nada.</summary>
public static class ReglasHorario
{
    /// <summary>Devuelve el mensaje de error, o null si el bloque es válido.</summary>
    public static string? ValidarBloque(string? materia, int diaSemana, TimeOnly inicio, TimeOnly fin)
    {
        if (string.IsNullOrWhiteSpace(materia)) return "La materia es obligatoria.";
        if (materia.Trim().Length > 100) return "La materia no puede superar 100 caracteres.";
        if (diaSemana < 1 || diaSemana > 6) return "El día debe ir de 1 (lunes) a 6 (sábado).";
        if (fin <= inicio) return "La hora de fin debe ser posterior a la hora de inicio.";
        return null;
    }

    /// <summary>Dos bloques se solapan si se cruzan; si uno termina justo cuando empieza el otro, NO.</summary>
    public static bool Solapa(TimeOnly aIni, TimeOnly aFin, TimeOnly bIni, TimeOnly bFin)
        => aIni < bFin && bIni < aFin;
}
