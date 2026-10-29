namespace Breaku.Application.Horarios;

/// <summary>Lee las clases de la foto de un horario con un modelo de visión. No guarda nada.</summary>
public interface ILectorHorarioFoto
{
    /// <summary>Devuelve las clases tal como las leyó el modelo; FotoHorarioService las valida después.</summary>
    /// <exception cref="ReglaNegocioException">Con <c>LECTOR_NO_DISPONIBLE</c> si no hay modelo configurado o no respondió.</exception>
    Task<List<ClaseLeidaDto>> LeerAsync(byte[] imagen, string tipoMime, CancellationToken ct = default);
}
