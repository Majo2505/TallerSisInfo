using Breaku.Domain;

namespace Breaku.Application.Horarios;

public interface IHorarioRepository
{
    Task<List<Horario>> ListarPorUsuarioAsync(int usuarioId, CancellationToken ct = default);
    Task<Horario?> ObtenerAsync(int id, int usuarioId, CancellationToken ct = default);
    Task AgregarAsync(Horario horario, CancellationToken ct = default);
    void Eliminar(Horario horario);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}
