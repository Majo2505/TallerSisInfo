using Breaku.Domain;

namespace Breaku.Application.Actividades;

public interface IExtraccionRepository
{
    Task GuardarLogAsync(LogAuditoriaRag log, CancellationToken ct = default);

    Task<LogAuditoriaRag?> ObtenerLogAsync(long id, CancellationToken ct = default);
}
