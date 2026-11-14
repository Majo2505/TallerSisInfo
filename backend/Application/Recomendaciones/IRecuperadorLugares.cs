using Breaku.Domain;

namespace Breaku.Application.Recomendaciones;

public interface IRecuperadorLugares
{
    Task<ContextoRecuperado> RecuperarAsync(int usuarioId, Puente puente, CancellationToken ct = default);
}

public record ContextoRecuperado(int? ZonaId, int? RadioAplicadoM, List<LugarCandidato> Lugares);

public record LugarCandidato(
    int LugarId,
    string Nombre,
    string Categoria,
    string? Descripcion,
    int DistanciaM,
    int TiempoTrasladoMin,
    int DuracionEstimadaMin,
    bool Validado = true,
    string? TipoLugar = null,
    List<string>? Modalidad = null,
    List<string>? NivelEnergia = null,
    List<string>? ActividadesSugeridas = null,
    string? FotoUrl = null,
    decimal? Puntuacion = null,
    int Votos = 0
);
