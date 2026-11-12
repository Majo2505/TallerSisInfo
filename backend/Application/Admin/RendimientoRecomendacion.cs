using Breaku.Domain.Rules;

namespace Breaku.Application.Admin;

/// <summary>HU19: la tarjeta de rendimiento de Métricas (P50, P95, % a tiempo y respuestas del plan B).</summary>
public static class RendimientoRecomendacion
{
    public static RendimientoRecomendacionDto Calcular(IReadOnlyCollection<(int LatenciaMs, bool Respaldo)> recomendaciones)
    {
        var tiempos = recomendaciones.Select(r => r.LatenciaMs).ToList();
        int? aTiempo = tiempos.Count == 0
            ? null
            : (int)Math.Round(100.0 * tiempos.Count(ReglasRendimiento.DentroDelObjetivo) / tiempos.Count);
        return new RendimientoRecomendacionDto(
            tiempos.Count,
            ReglasRendimiento.ObjetivoRecomendacionMs,
            ReglasRendimiento.Percentil(tiempos, 0.5),
            ReglasRendimiento.Percentil(tiempos, 0.95),
            aTiempo,
            recomendaciones.Count(r => r.Respaldo));
    }
}
