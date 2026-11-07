namespace Breaku.Domain.Rules;

public static class ReglasMatching
{
    public const int MinutosMinimosEnComun = 30;

    public const int MaximoCompaneros = 20;

    public const string MensajeSinZona = "Elige tu campus para encontrar compañeros con tiempo libre compatible.";
    public const string MensajeNoVisible = "Para ver a tus compañeros, primero acepta que ellos también te encuentren.";
    public const string MensajeSinResultados = "Por ahora nadie de tu campus está libre contigo a esa hora.";

    public readonly record struct Franja(int DiaSemana, TimeOnly Inicio, TimeOnly Fin)
    {
        public int Minutos => (int)(Fin.ToTimeSpan() - Inicio.ToTimeSpan()).TotalMinutes;
    }

    public readonly record struct PuenteDe(int UsuarioId, Franja Franja);

    public readonly record struct Compatibilidad(int UsuarioId, Franja EnComun, int MinutosEnComun, int Porcentaje, int FranjasCompartidas);

    public static Franja? Cruce(Franja mia, Franja otra, int minutosMinimos = MinutosMinimosEnComun)
    {
        if (mia.DiaSemana != otra.DiaSemana) return null;

        var inicio = mia.Inicio > otra.Inicio ? mia.Inicio : otra.Inicio;
        var fin = mia.Fin < otra.Fin ? mia.Fin : otra.Fin;
        if (fin <= inicio) return null;

        var enComun = new Franja(mia.DiaSemana, inicio, fin);
        return enComun.Minutos >= minutosMinimos ? enComun : null;
    }

    public static int Porcentaje(Franja mia, int minutosEnComun)
    {
        if (mia.Minutos <= 0) return 0;
        var porcentaje = (int)Math.Round(100.0 * minutosEnComun / mia.Minutos, MidpointRounding.AwayFromZero);
        return Math.Clamp(porcentaje, 1, 100);
    }

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
