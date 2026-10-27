using Breaku.Domain;

namespace Breaku.Application.Recomendaciones;

/// <summary>
/// Puerto de recuperación del RAG: lo implementa HU6 (filtrar el catálogo por tiempo, cercanía y zona segura).
/// Mientras HU6 no esté lista, Infrastructure registra <c>RecuperadorLugaresProvisional</c>.
/// </summary>
public interface IRecuperadorLugares
{
    /// <summary>
    /// Lugares del catálogo viables para el puente: solo ACTIVO (aprobados por el administrador), dentro del radio
    /// de la zona y cuyo traslado de ida y vuelta más la actividad caben en el puente. Lista vacía si no hay ninguno.
    /// </summary>
    Task<ContextoRecuperado> RecuperarAsync(int usuarioId, Puente puente, CancellationToken ct = default);
}

/// <summary>Resultado de la búsqueda. <paramref name="ZonaId"/> y <paramref name="RadioAplicadoM"/> se copian al log.</summary>
public record ContextoRecuperado(int? ZonaId, int? RadioAplicadoM, List<LugarCandidato> Lugares);

/// <summary>Un lugar real del catálogo, con las métricas ya calculadas por la búsqueda (no por el LLM).</summary>
public record LugarCandidato(
    int LugarId,
    string Nombre,
    string Categoria,
    string? Descripcion,
    int DistanciaM,
    int TiempoTrasladoMin,      // ida y vuelta
    int DuracionEstimadaMin
);
