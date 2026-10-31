namespace Breaku.Domain.Rules;

/// <summary>
/// Reglas puras del matching de compañeros con puente compatible (HU12). No dependen de nada.
/// Cruce por franja horaria: dos puentes son compatibles si caen el mismo día y comparten al menos
/// <see cref="MinutosMinimosEnComun"/> minutos. Orden por compatibilidad: más minutos en común primero;
/// en empate, quien coincide en más franjas de la semana.
/// </summary>
public static class ReglasMatching
{
    /// <summary>Con menos tiempo en común no da para hacer nada juntos.</summary>
    public const int MinutosMinimosEnComun = 30;

    /// <summary>Tope de compañeros que se muestran por puente.</summary>
    public const int MaximoCompaneros = 20;

    public const string MensajeSinZona = "Elige tu campus para encontrar compañeros con tiempo libre compatible.";
    public const string MensajeNoVisible = "Para ver a tus compañeros, primero acepta que ellos también te encuentren.";
    public const string MensajeSinResultados = "Por ahora nadie de tu campus está libre contigo en este puente.";

    public readonly record struct Franja(int DiaSemana, TimeOnly Inicio, TimeOnly Fin)
    {
        public int Minutos => (int)(Fin.ToTimeSpan() - Inicio.ToTimeSpan()).TotalMinutes;
    }

    /// <summary>Puente de otro estudiante, sin datos personales: solo a quién pertenece y cuándo es.</summary>
    public readonly record struct PuenteDe(int UsuarioId, Franja Franja);

    /// <summary>Resultado del cruce con un compañero.</summary>
    /// <param name="UsuarioId">El compañero.</param>
    /// <param name="MinutosEnComun">Duración de <paramref name="EnComun"/>.</param>
    /// <param name="EnComun">El tramo compartido más largo dentro de mi puente.</param>
    /// <param name="Porcentaje">Qué parte de mi puente cubre ese tramo, de 1 a 100.</param>
    /// <param name="FranjasCompartidas">En cuántos de mis puentes de la semana coincidimos (incluye el consultado).</param>
    public readonly record struct Compatibilidad(int UsuarioId, Franja EnComun, int MinutosEnComun, int Porcentaje, int FranjasCompartidas);

    /// <summary>Tramo en común de dos franjas, o <c>null</c> si no caen el mismo día o comparten menos del mínimo.</summary>
    public static Franja? Cruce(Franja mia, Franja otra, int minutosMinimos = MinutosMinimosEnComun)
    {
        if (mia.DiaSemana != otra.DiaSemana) return null;

        var inicio = mia.Inicio > otra.Inicio ? mia.Inicio : otra.Inicio;
        var fin = mia.Fin < otra.Fin ? mia.Fin : otra.Fin;
        if (fin <= inicio) return null;

        var enComun = new Franja(mia.DiaSemana, inicio, fin);
        return enComun.Minutos >= minutosMinimos ? enComun : null;
    }

    /// <summary>Qué parte de mi puente cubre el tramo en común, redondeado y entre 1 y 100.</summary>
    public static int Porcentaje(Franja mia, int minutosEnComun)
    {
        if (mia.Minutos <= 0) return 0;
        var porcentaje = (int)Math.Round(100.0 * minutosEnComun / mia.Minutos, MidpointRounding.AwayFromZero);
        return Math.Clamp(porcentaje, 1, 100);
    }

    /// <summary>
    /// Cruza mi puente con los de mis compañeros y los ordena por compatibilidad.
    /// Si un compañero tiene dos puentes que tocan el mío, cuenta el tramo más largo.
    /// <paramref name="misPuentes"/> es toda mi semana (para contar las franjas compartidas) y debe incluir a <paramref name="mia"/>.
    /// En empate total se ordena por <c>UsuarioId</c>, para que el resultado sea siempre el mismo.
    /// </summary>
    public static List<Compatibilidad> Calcular(Franja mia, IReadOnlyCollection<Franja> misPuentes, IEnumerable<PuenteDe> puentesDeOtros)
    {
        var resultado = new List<Compatibilidad>();

        foreach (var companero in puentesDeOtros.GroupBy(p => p.UsuarioId))
        {
            var suyos = companero.Select(p => p.Franja).ToList();

            var mejor = suyos
                .Select(f => Cruce(mia, f))
                .Where(c => c is not null)
                .Select(c => c!.Value)
                .OrderByDescending(c => c.Minutos)
                .ThenBy(c => c.Inicio)
                .Cast<Franja?>()
                .FirstOrDefault();
            if (mejor is null) continue;

            var franjasCompartidas = misPuentes.Count(m => suyos.Any(s => Cruce(m, s) is not null));
            var minutos = mejor.Value.Minutos;
            resultado.Add(new Compatibilidad(companero.Key, mejor.Value, minutos, Porcentaje(mia, minutos), Math.Max(franjasCompartidas, 1)));
        }

        return resultado
            .OrderByDescending(c => c.MinutosEnComun)
            .ThenByDescending(c => c.FranjasCompartidas)
            .ThenBy(c => c.UsuarioId)
            .ToList();
    }
}
