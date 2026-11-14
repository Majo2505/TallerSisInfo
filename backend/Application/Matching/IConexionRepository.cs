namespace Breaku.Application.Matching;

public record TestGuardado(int Version, Dictionary<string, List<string>> Respuestas, DateTime ActualizadoEn);

/// <summary>HU12: las respuestas del test de afinidad (tabla <c>test_afinidad</c>).</summary>
public interface IAfinidadRepository
{
    Task<TestGuardado?> ObtenerTestAsync(int usuarioId, CancellationToken ct = default);

    /// <summary>Crea o reemplaza el test del usuario. Si ya existía, conserva <c>completado_en</c>.</summary>
    Task GuardarTestAsync(int usuarioId, int version, Dictionary<string, List<string>> respuestas, DateTime ahoraUtc, CancellationToken ct = default);

    /// <summary>Los tests de esos usuarios hechos con la versión indicada (los de otra versión no cuentan).</summary>
    Task<Dictionary<int, Dictionary<string, List<string>>>> ObtenerTestsAsync(IReadOnlyCollection<int> usuarioIds, int version, CancellationToken ct = default);
}

public record ConexionGuardada(
    int Id, int SolicitanteId, int DestinatarioId, int DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin,
    int Afinidad, List<string> EnComun, string Estado, DateTime CreadaEn, DateTime? RespondidaEn);

public record PersonaConexion(int Id, string Nombre, string? Carrera, int? Semestre, string Correo);

public record ConexionNueva(
    int SolicitanteId, int DestinatarioId, int DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin,
    int Afinidad, List<string> EnComun);

/// <summary>HU12: los saludos entre estudiantes (tabla <c>conexion</c>) y sus avisos.</summary>
public interface IConexionRepository
{
    /// <summary>Todas las conexiones donde participa el usuario, enviadas o recibidas.</summary>
    Task<List<ConexionGuardada>> ListarDeUsuarioAsync(int usuarioId, CancellationToken ct = default);

    Task<ConexionGuardada?> ObtenerAsync(int id, CancellationToken ct = default);

    /// <summary>La conexión entre dos personas, en cualquier sentido.</summary>
    Task<ConexionGuardada?> EntreAsync(int usuarioA, int usuarioB, CancellationToken ct = default);

    Task<int> ContarPendientesEnviadasAsync(int usuarioId, CancellationToken ct = default);

    Task<Dictionary<int, PersonaConexion>> ObtenerPersonasAsync(IReadOnlyCollection<int> usuarioIds, CancellationToken ct = default);

    /// <summary>
    /// Guarda el saludo y el aviso CONEXION_SOLICITADA para el destinatario (solo si tiene los avisos encendidos), en una
    /// transacción. Si la pareja ya tenía una conexión (dos saludos a la vez), lanza <c>ReglaNegocioException</c> YA_EXISTE.
    /// </summary>
    Task<int> CrearAsync(ConexionNueva conexion, string mensajeAviso, DateTime ahoraUtc, CancellationToken ct = default);

    /// <summary>
    /// Acepta o rechaza: cambia el estado, apaga el aviso que recibió el destinatario y, si acepta, avisa al solicitante
    /// (CONEXION_ACEPTADA, solo si tiene los avisos encendidos). Todo en una transacción.
    /// </summary>
    Task ResponderAsync(int conexionId, bool aceptar, string? mensajeAvisoAceptada, DateTime ahoraUtc, CancellationToken ct = default);
}
