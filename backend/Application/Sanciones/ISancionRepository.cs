using Breaku.Domain.Rules;

namespace Breaku.Application.Sanciones;

public sealed record DatosUsuario(int Id, string Nombre, string Correo, string? Carrera, byte? Semestre, string Rol, bool Activo, DateTime CreadoEn);

public sealed record RegistroDeUsuario(int UsuarioId, ReglasSancion.Registro Registro);

/// <summary>Una sanción con lo que muestra el historial: quién la aplicó y quién la levantó.</summary>
public sealed record SancionDetalle(ReglasSancion.Registro Registro, int? ReporteId, string CreadoPor, string? LevantadaPor);

/// <summary>Lo que necesita la validación por request: si el usuario existe y por qué estaría bloqueado.</summary>
public sealed record EstadoSesion(bool Existe, ReglasSancion.BloqueoCuenta? Bloqueo);

public interface ISancionRepository
{
    /// <summary>Estudiantes (nunca administradores) cuyo nombre o correo contiene el texto, por nombre.</summary>
    Task<List<DatosUsuario>> ListarEstudiantesAsync(string? buscar, int limite, CancellationToken ct = default);

    Task<List<RegistroDeUsuario>> ListarRegistrosAsync(IReadOnlyCollection<int> usuarioIds, CancellationToken ct = default);

    Task<DatosUsuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default);

    Task<List<SancionDetalle>> ListarSancionesAsync(int usuarioId, CancellationToken ct = default);

    Task<SancionDetalle?> ObtenerSancionAsync(int usuarioId, int sancionId, CancellationToken ct = default);

    Task<bool> ReporteExisteAsync(int reporteId, CancellationToken ct = default);

    /// <summary>Dentro de la transacción: bloquea la fila del usuario para que dos sanciones a la vez no se pisen.</summary>
    Task BloquearUsuarioAsync(int usuarioId, CancellationToken ct = default);

    /// <summary>Guarda la sanción (quién y cuándo) y devuelve su id.</summary>
    Task<int> AgregarAsync(int usuarioId, ReglasSancion.SancionValida sancion, int? reporteId, int adminId, DateTime ahoraUtc,
        CancellationToken ct = default);

    /// <summary>Marca la sanción como levantada (quién y cuándo). false si ya estaba levantada.</summary>
    Task<bool> LevantarAsync(int sancionId, int adminId, DateTime ahoraUtc, CancellationToken ct = default);

    /// <summary>El aviso de la campanita por una advertencia (SANCION_ADVERTENCIA).</summary>
    Task AgregarAvisoAdvertenciaAsync(int usuarioId, int sancionId, string mensaje, DateTime ahoraUtc, CancellationToken ct = default);

    /// <summary>Null si el usuario no existe.</summary>
    Task<EstadoSesion?> ObtenerEstadoSesionAsync(int usuarioId, DateTime ahoraUtc, CancellationToken ct = default);
}

/// <summary>Una transacción de base de datos. Si ya hay una abierta, el trabajo se une a ella.</summary>
public interface ITransaccion
{
    Task<T> EjecutarAsync<T>(Func<Task<T>> trabajo, CancellationToken ct = default);
}
