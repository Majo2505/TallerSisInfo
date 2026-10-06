using Breaku.Domain;

namespace Breaku.Application.Horarios;

/// <summary>Puerto: lo implementa Infrastructure.</summary>
public interface IHorarioRepository
{
    Task<List<Horario>> ListarPorUsuarioAsync(int usuarioId, CancellationToken ct = default);
    Task<Horario?> ObtenerAsync(int id, int usuarioId, CancellationToken ct = default);
    Task AgregarAsync(Horario horario, CancellationToken ct = default);
    void Eliminar(Horario horario);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}
