using Breaku.Domain;

namespace Breaku.Application.Matching;

/// <summary>Compañero candidato con todos sus puentes de la semana.</summary>
public record CompaneroCandidato(int UsuarioId, string Nombre, string? Carrera, byte? Semestre, List<Puente> Puentes);

public interface IMatchingRepository
{
    /// <summary>Usuario activo con su sede y seguimiento de cambios, o <c>null</c>.</summary>
    Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default);

    /// <summary>
    /// Estudiantes activos de la sede que aceptaron aparecer y tienen al menos un puente que se cruza con
    /// <paramref name="puente"/> (mismo día, horas que se solapan). Excluye al propio usuario.
    /// </summary>
    Task<List<CompaneroCandidato>> ListarCandidatosAsync(int usuarioId, int zonaId, Puente puente, CancellationToken ct = default);

    /// <summary>Guarda los cambios del usuario y deja el antes y el después en historial_cambio, en una transacción.</summary>
    Task GuardarConHistorialAsync(int usuarioId, string valoresAnteriores, string valoresNuevos, CancellationToken ct = default);
}
