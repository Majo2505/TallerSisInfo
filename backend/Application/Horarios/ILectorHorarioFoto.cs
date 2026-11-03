namespace Breaku.Application.Horarios;

public interface ILectorHorarioFoto
{
    Task<List<ClaseLeidaDto>> LeerAsync(byte[] imagen, string tipoMime, CancellationToken ct = default);
}
