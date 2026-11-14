using System.Text.Json;
using Breaku.Application.Horarios;
using Breaku.Application.Matching;
using Breaku.Domain;
using Breaku.Domain.Rules;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Breaku.Infrastructure.Persistence;

/// <summary>HU12: el test de afinidad (tabla <c>test_afinidad</c>). Las respuestas se guardan como objeto JSON.</summary>
public sealed class AfinidadRepository : IAfinidadRepository
{
    private readonly AppDbContext _db;
    public AfinidadRepository(AppDbContext db) => _db = db;

    public async Task<TestGuardado?> ObtenerTestAsync(int usuarioId, CancellationToken ct = default)
    {
        var fila = (await _db.Database.SqlQuery<FilaTest>($"""
                SELECT usuario_id AS UsuarioId, CAST(version AS SIGNED) AS Version, CAST(respuestas AS CHAR) AS Respuestas,
                       actualizado_en AS ActualizadoEn
                  FROM test_afinidad WHERE usuario_id = {usuarioId}
                """).ToListAsync(ct)).SingleOrDefault();
        return fila is null ? null : new TestGuardado((int)fila.Version, Leer(fila.Respuestas), fila.ActualizadoEn);
    }

    public Task GuardarTestAsync(int usuarioId, int version, Dictionary<string, List<string>> respuestas, DateTime ahoraUtc,
        CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(respuestas);
        return _db.Database.ExecuteSqlAsync($"""
                INSERT INTO test_afinidad (usuario_id, version, respuestas, completado_en, actualizado_en)
                VALUES ({usuarioId}, {version}, CAST({json} AS JSON), {ahoraUtc}, {ahoraUtc})
                ON DUPLICATE KEY UPDATE version = {version}, respuestas = CAST({json} AS JSON), actualizado_en = {ahoraUtc}
                """, ct);
    }

    public async Task<Dictionary<int, Dictionary<string, List<string>>>> ObtenerTestsAsync(
        IReadOnlyCollection<int> usuarioIds, int version, CancellationToken ct = default)
    {
        if (usuarioIds.Count == 0) return [];
        // Los ids son enteros: armar el IN a mano no abre la puerta a inyección.
        var filas = await _db.Database.SqlQueryRaw<FilaTest>($"""
                SELECT usuario_id AS UsuarioId, CAST(version AS SIGNED) AS Version, CAST(respuestas AS CHAR) AS Respuestas,
                       actualizado_en AS ActualizadoEn
                  FROM test_afinidad
                 WHERE version = {version} AND usuario_id IN ({string.Join(",", usuarioIds.Distinct())})
                """).ToListAsync(ct);
        return filas.ToDictionary(f => f.UsuarioId, f => Leer(f.Respuestas));
    }

    private static Dictionary<string, List<string>> Leer(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json) ?? [];

    private sealed class FilaTest
    {
        public int UsuarioId { get; set; }
        public long Version { get; set; }
        public string Respuestas { get; set; } = "{}";
        public DateTime ActualizadoEn { get; set; }
    }
}

/// <summary>HU12: los saludos entre estudiantes (tabla <c>conexion</c>) y sus avisos, con SQL explícito.</summary>
public sealed class ConexionRepository : IConexionRepository
{
    private readonly AppDbContext _db;
    public ConexionRepository(AppDbContext db) => _db = db;

    private const string Columnas = """
        SELECT id AS Id, solicitante_id AS SolicitanteId, destinatario_id AS DestinatarioId,
               CAST(dia_semana AS SIGNED) AS DiaSemana, hora_inicio AS HoraInicio, hora_fin AS HoraFin,
               CAST(afinidad AS SIGNED) AS Afinidad,
               CAST(en_comun AS CHAR) AS EnComun, estado AS Estado, creada_en AS CreadaEn, respondida_en AS RespondidaEn
          FROM conexion
        """;

    public async Task<List<ConexionGuardada>> ListarDeUsuarioAsync(int usuarioId, CancellationToken ct = default)
        => await Consultar("WHERE solicitante_id = {0} OR destinatario_id = {0}", ct, usuarioId);

    public async Task<ConexionGuardada?> ObtenerAsync(int id, CancellationToken ct = default)
        => (await Consultar("WHERE id = {0}", ct, id)).SingleOrDefault();

    public async Task<ConexionGuardada?> EntreAsync(int usuarioA, int usuarioB, CancellationToken ct = default)
        => (await Consultar("WHERE par_menor = {0} AND par_mayor = {1}", ct, Math.Min(usuarioA, usuarioB), Math.Max(usuarioA, usuarioB)))
            .SingleOrDefault();

    public async Task<int> ContarPendientesEnviadasAsync(int usuarioId, CancellationToken ct = default)
        => (int)(await _db.Database.SqlQuery<long>($"""
                SELECT COUNT(*) AS Value FROM conexion WHERE solicitante_id = {usuarioId} AND estado = 'PENDIENTE'
                """).ToListAsync(ct)).Single();

    public async Task<Dictionary<int, PersonaConexion>> ObtenerPersonasAsync(IReadOnlyCollection<int> usuarioIds, CancellationToken ct = default)
    {
        if (usuarioIds.Count == 0) return [];
        var filas = await _db.Database.SqlQueryRaw<FilaPersona>($"""
                SELECT id AS Id, nombre AS Nombre, carrera AS Carrera, CAST(semestre AS SIGNED) AS Semestre, correo AS Correo
                  FROM usuario WHERE id IN ({string.Join(",", usuarioIds.Distinct())})
                """).ToListAsync(ct);
        return filas.ToDictionary(f => f.Id, f => new PersonaConexion(f.Id, f.Nombre, f.Carrera, (int?)f.Semestre, f.Correo));
    }

    public async Task<int> CrearAsync(ConexionNueva c, string mensajeAviso, DateTime ahoraUtc, CancellationToken ct = default)
    {
        var enComun = JsonSerializer.Serialize(c.EnComun);
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _db.Database.ExecuteSqlAsync($"""
                    INSERT INTO conexion (solicitante_id, destinatario_id, dia_semana, hora_inicio, hora_fin, afinidad, en_comun,
                                          estado, creada_en)
                    VALUES ({c.SolicitanteId}, {c.DestinatarioId}, {c.DiaSemana}, {c.HoraInicio.ToTimeSpan()},
                            {c.HoraFin.ToTimeSpan()}, {c.Afinidad}, CAST({enComun} AS JSON), {ReglasConexion.Pendiente},
                            {ahoraUtc})
                    """, ct);
        }
        catch (Exception ex) when ((ex as MySqlException ?? ex.InnerException as MySqlException)
                                   is { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
        {
            // Los dos se saludaron al mismo tiempo: uq_conexion_par deja pasar solo uno.
            throw new ReglaNegocioException(ReglasConexion.ErrorYaExiste, ReglasConexion.MensajeYaExiste(ReglasConexion.Pendiente, false));
        }

        var id = (int)(await _db.Database.SqlQuery<long>($"SELECT LAST_INSERT_ID() AS Value").ToListAsync(ct)).Single();
        await AvisarAsync(c.DestinatarioId, Notificacion.TipoConexionSolicitada, id, mensajeAviso, ahoraUtc, ct);
        await tx.CommitAsync(ct);
        return id;
    }

    public async Task ResponderAsync(int conexionId, bool aceptar, string? mensajeAvisoAceptada, DateTime ahoraUtc,
        CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var estado = aceptar ? ReglasConexion.Aceptada : ReglasConexion.Rechazada;
        var cambiadas = await _db.Database.ExecuteSqlAsync($"""
                UPDATE conexion SET estado = {estado}, respondida_en = {ahoraUtc}
                 WHERE id = {conexionId} AND estado = 'PENDIENTE'
                """, ct);
        // Otra pestaña respondió primero.
        if (cambiadas == 0) throw new ReglaNegocioException(ReglasConexion.ErrorYaRespondida, ReglasConexion.MensajeYaRespondida);

        // El aviso del saludo ya cumplió: se apaga para el destinatario.
        await _db.Database.ExecuteSqlAsync($"""
                UPDATE notificacion SET leida = TRUE, activa = FALSE
                 WHERE conexion_id = {conexionId} AND tipo = {Notificacion.TipoConexionSolicitada}
                """, ct);

        if (aceptar && mensajeAvisoAceptada is not null)
        {
            var solicitante = (await _db.Database.SqlQuery<int>($"SELECT solicitante_id AS Value FROM conexion WHERE id = {conexionId}")
                .ToListAsync(ct)).Single();
            await AvisarAsync(solicitante, Notificacion.TipoConexionAceptada, conexionId, mensajeAvisoAceptada, ahoraUtc, ct);
        }

        await tx.CommitAsync(ct);
    }

    /// <summary>Como el resto de los avisos: no se crea si la persona apagó las notificaciones.</summary>
    private Task AvisarAsync(int usuarioId, string tipo, int conexionId, string mensaje, DateTime ahoraUtc, CancellationToken ct)
        => _db.Database.ExecuteSqlAsync($"""
                INSERT INTO notificacion (usuario_id, tipo, conexion_id, mensaje, leida, activa, created_at)
                SELECT id, {tipo}, {conexionId}, {mensaje}, FALSE, TRUE, {ahoraUtc}
                  FROM usuario WHERE id = {usuarioId} AND notificaciones_activas = TRUE
                """, ct);

    /// <summary>Las columnas son fijas; el filtro lleva marcadores {0}, {1}… que EF manda como parámetros.</summary>
    private async Task<List<ConexionGuardada>> Consultar(string filtro, CancellationToken ct, params object[] valores)
    {
        var filas = await _db.Database.SqlQueryRaw<FilaConexion>($"{Columnas} {filtro}", valores).ToListAsync(ct);
        return filas.Select(f => new ConexionGuardada(
                f.Id, f.SolicitanteId, f.DestinatarioId, (int)f.DiaSemana, TimeOnly.FromTimeSpan(f.HoraInicio),
                TimeOnly.FromTimeSpan(f.HoraFin), (int)f.Afinidad,
                JsonSerializer.Deserialize<List<string>>(f.EnComun) ?? [], f.Estado, f.CreadaEn, f.RespondidaEn))
            .ToList();
    }

    private sealed class FilaConexion
    {
        public int Id { get; set; }
        public int SolicitanteId { get; set; }
        public int DestinatarioId { get; set; }
        public long DiaSemana { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public long Afinidad { get; set; }
        public string EnComun { get; set; } = "[]";
        public string Estado { get; set; } = "";
        public DateTime CreadaEn { get; set; }
        public DateTime? RespondidaEn { get; set; }
    }

    private sealed class FilaPersona
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string? Carrera { get; set; }
        public long? Semestre { get; set; }
        public string Correo { get; set; } = "";
    }
}
