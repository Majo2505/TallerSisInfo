using Breaku.Domain;

namespace Breaku.Application.Actividades;

/// <summary>Puerto: lo implementa Infrastructure.</summary>
public interface IExtraccionRepository
{
    /// <summary>Guarda el registro de auditoría de la extracción. Al volver, <c>log.Id</c> ya está asignado.</summary>
    Task GuardarLogAsync(LogAuditoriaRag log, CancellationToken ct = default);
}
