using Breaku.Domain;

namespace Breaku.Application.Admin;

/// <summary>Un turno guardado en <c>log_auditoria_rag</c>, con la conversación a la que pertenece.</summary>
public record TurnoConsultaRag(
    long Id,
    long Conversacion,
    DateTime Fecha,
    string Usuario,
    string Tipo,
    string? Entrada,
    string Estado,
    string? MotivoRechazo,
    string? Modelo,
    int? LatenciaMs,
    int LugaresRecuperados,
    int Recomendados);

/// <summary>
/// Junta los turnos de una misma conversación con el asistente en una sola consulta para la Auditoría RAG. Al crear una
/// actividad, el asistente pregunta lo que falta («¿A qué hora?») y la respuesta («5») es otro turno: suelto no se entendía.
/// </summary>
public static class AuditoriaConversaciones
{
    public static List<ConsultaRagAdminDto> Agrupar(IEnumerable<TurnoConsultaRag> turnos)
        => turnos
            .GroupBy(t => t.Conversacion)
            .Select(g => Resumir(g.OrderBy(t => t.Fecha).ThenBy(t => t.Id).ToList()))
            .OrderByDescending(c => c.Fecha)
            .ThenByDescending(c => c.Id)
            .ToList();

    private static ConsultaRagAdminDto Resumir(List<TurnoConsultaRag> t)
    {
        var primero = t[0];
        var ultimo = t[^1];
        // Si algún turno se rechazó por seguridad, la conversación se ve como rechazada: un intento de prompt injection
        // en el medio no puede quedar escondido detrás de un «OK» final.
        var rechazado = t.LastOrDefault(x => x.Estado == LogAuditoriaRag.EstadoRechazadoSeguridad);
        var latencias = t.Where(x => x.LatenciaMs is not null).Select(x => x.LatenciaMs!.Value).ToList();

        return new ConsultaRagAdminDto(
            primero.Conversacion,
            ultimo.Fecha,
            primero.Usuario,
            primero.Tipo,
            primero.Entrada,
            rechazado?.Estado ?? ultimo.Estado,
            t.LastOrDefault(x => x.Modelo is not null)?.Modelo,
            latencias.Count == 0 ? null : latencias.Sum(),
            t.Sum(x => x.LugaresRecuperados),
            t.Sum(x => x.Recomendados),
            rechazado?.MotivoRechazo,
            t.Skip(1).Where(x => !string.IsNullOrWhiteSpace(x.Entrada)).Select(x => x.Entrada!).ToList(),
            t.Count);
    }
}
