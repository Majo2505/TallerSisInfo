using Breaku.Domain;
using Breaku.Domain.Rules;
using Breaku.Application.Puentes;

namespace Breaku.Application.Horarios;

/// <summary>
/// Casos de uso de HU2: alta, edición y eliminación de bloques, con persistencia por usuario.
/// Dos clases pueden coincidir en la misma franja (el horario oficial lo permite): el choque no se bloquea, se avisa,
/// y el estudiante marca a cuáles asiste. Solo los bloques a los que asiste cuentan como tiempo ocupado.
/// </summary>
public class HorarioService
{
    public const int MaximoBloquesPorLote = 40;
    private static readonly string[] Formatos = { "HH:mm", "HH:mm:ss" };

    private readonly IHorarioRepository _repo;
    private readonly PuenteService _puentes;

    public HorarioService(IHorarioRepository repo, PuenteService puentes)
    {
        _repo = repo;
        _puentes = puentes;
    }

    public async Task<List<HorarioDto>> ListarAsync(int usuarioId, CancellationToken ct = default)
    {
        var bloques = await _repo.ListarPorUsuarioAsync(usuarioId, ct);
        return bloques.OrderBy(b => b.DiaSemana).ThenBy(b => b.HoraInicio).Select(b => ADto(b, bloques)).ToList();
    }

    public async Task<HorarioDto> CrearAsync(int usuarioId, GuardarHorarioRequest req, CancellationToken ct = default)
    {
        var (ini, fin) = ParsearYValidar(req);
        await VerificarDuplicadoAsync(usuarioId, req.DiaSemana, ini, req.Materia, null, ct);

        var ahora = DateTime.UtcNow;
        var h = new Horario
        {
            UsuarioId = usuarioId, // siempre del JWT, nunca del body
            Materia = req.Materia.Trim(), DiaSemana = req.DiaSemana,
            HoraInicio = ini, HoraFin = fin, Aula = LimpiarAula(req.Aula),
            Origen = "MANUAL", Asiste = req.Asiste ?? true, AutorizacionChoque = req.AutorizacionCarrera ?? false,
            CreatedAt = ahora, UpdatedAt = ahora
        };
        await _repo.AgregarAsync(h, ct);
        await _repo.GuardarCambiosAsync(ct);
        await _puentes.RecalcularAsync(usuarioId, ct);
        return ADto(h, await _repo.ListarPorUsuarioAsync(usuarioId, ct));
    }

    /// <summary>
    /// Guarda varios bloques de una vez (los confirmados desde la foto del horario, con origen <c>FOTO</c>).
    /// Cada uno se valida igual que un alta manual; los inválidos o repetidos se devuelven como rechazados y el resto
    /// se guarda. Los que coinciden con otra clase se guardan igual: cada bloque guardado trae sus coincidencias y
    /// advertencias. Los puentes se recalculan una sola vez.
    /// </summary>
    public async Task<ResultadoLoteDto> CrearLoteAsync(int usuarioId, GuardarLoteRequest req, CancellationToken ct = default)
    {
        if (req.Bloques is null || req.Bloques.Count == 0)
            throw new ReglaNegocioException("VALIDACION", "Envía al menos un bloque.");
        if (req.Bloques.Count > MaximoBloquesPorLote)
            throw new ReglaNegocioException("VALIDACION", $"Se pueden guardar hasta {MaximoBloquesPorLote} bloques a la vez.");

        var existentes = (await _repo.ListarPorUsuarioAsync(usuarioId, ct))
            .Select(b => (b.DiaSemana, b.HoraInicio, b.Materia)).ToList();
        var nuevos = new List<Horario>();
        var rechazados = new List<BloqueRechazadoDto>();
        var ahora = DateTime.UtcNow;

        for (var i = 0; i < req.Bloques.Count; i++)
        {
            var b = req.Bloques[i];
            try
            {
                var (ini, fin) = ParsearYValidar(b);
                var materia = b.Materia.Trim();
                if (existentes.Any(o => EsMismoBloque(o.DiaSemana, o.HoraInicio, o.Materia, b.DiaSemana, ini, materia)))
                    throw new ReglaNegocioException("DUPLICADO", MensajeDuplicado(materia));

                nuevos.Add(new Horario
                {
                    UsuarioId = usuarioId, // siempre del JWT, nunca del body
                    Materia = materia, DiaSemana = b.DiaSemana,
                    HoraInicio = ini, HoraFin = fin, Aula = LimpiarAula(b.Aula),
                    Origen = "FOTO", Asiste = b.Asiste ?? true, AutorizacionChoque = b.AutorizacionCarrera ?? false,
                    CreatedAt = ahora, UpdatedAt = ahora
                });
                existentes.Add((b.DiaSemana, ini, materia));
            }
            catch (ReglaNegocioException ex)
            {
                rechazados.Add(new BloqueRechazadoDto(i, b.Materia?.Trim() ?? "", ex.Message));
            }
        }

        if (nuevos.Count == 0) return new ResultadoLoteDto([], rechazados);

        foreach (var h in nuevos) await _repo.AgregarAsync(h, ct);
        await _repo.GuardarCambiosAsync(ct);
        await _puentes.RecalcularAsync(usuarioId, ct);
        var todos = await _repo.ListarPorUsuarioAsync(usuarioId, ct);
        return new ResultadoLoteDto(nuevos.Select(h => ADto(h, todos)).ToList(), rechazados);
    }

    public async Task<HorarioDto> EditarAsync(int usuarioId, int id, GuardarHorarioRequest req, CancellationToken ct = default)
    {
        var h = await _repo.ObtenerAsync(id, usuarioId, ct)
                ?? throw new ReglaNegocioException("NO_ENCONTRADO", "El bloque no existe.");
        var (ini, fin) = ParsearYValidar(req);
        await VerificarDuplicadoAsync(usuarioId, req.DiaSemana, ini, req.Materia, id, ct);

        h.Materia = req.Materia.Trim(); h.DiaSemana = req.DiaSemana;
        h.HoraInicio = ini; h.HoraFin = fin; h.Aula = LimpiarAula(req.Aula);
        if (req.Asiste is not null) h.Asiste = req.Asiste.Value;
        if (req.AutorizacionCarrera is not null) h.AutorizacionChoque = req.AutorizacionCarrera.Value;
        h.UpdatedAt = DateTime.UtcNow;
        await _repo.GuardarCambiosAsync(ct);
        await _puentes.RecalcularAsync(usuarioId, ct);
        return ADto(h, await _repo.ListarPorUsuarioAsync(usuarioId, ct));
    }

    /// <summary>Marca si el estudiante asiste a un bloque. Los puentes se recalculan con los bloques a los que asiste.</summary>
    public async Task<HorarioDto> CambiarAsistenciaAsync(int usuarioId, int id, bool asiste, CancellationToken ct = default)
    {
        var resultado = await CambiarAsistenciasAsync(usuarioId, [new AsistenciaBloqueRequest(id, asiste)], ct);
        return resultado.Single();
    }

    /// <summary>Cambia la asistencia de varios bloques de una vez (el paso «¿a cuáles asistes?» tras importar la foto).</summary>
    public async Task<List<HorarioDto>> CambiarAsistenciasAsync(int usuarioId, List<AsistenciaBloqueRequest> cambios, CancellationToken ct = default)
    {
        if (cambios is null || cambios.Count == 0)
            throw new ReglaNegocioException("VALIDACION", "Envía al menos un bloque.");
        if (cambios.Count > MaximoBloquesPorLote)
            throw new ReglaNegocioException("VALIDACION", $"Se pueden cambiar hasta {MaximoBloquesPorLote} bloques a la vez.");

        var propios = (await _repo.ListarPorUsuarioAsync(usuarioId, ct)).ToDictionary(b => b.Id);
        // Solo bloques del propio usuario: uno ajeno responde igual que si no existiera (BOLA). Si falta alguno no se cambia ninguno.
        if (cambios.Any(c => !propios.ContainsKey(c.Id))) throw new ReglaNegocioException("NO_ENCONTRADO", "El bloque no existe.");

        var ahora = DateTime.UtcNow;
        foreach (var c in cambios)
        {
            var h = propios[c.Id];
            h.Asiste = c.Asiste;
            h.UpdatedAt = ahora;
        }

        await _repo.GuardarCambiosAsync(ct);
        await _puentes.RecalcularAsync(usuarioId, ct);
        var todos = await _repo.ListarPorUsuarioAsync(usuarioId, ct);
        return cambios.Select(c => ADto(todos.First(b => b.Id == c.Id), todos)).ToList();
    }

    public async Task EliminarAsync(int usuarioId, int id, CancellationToken ct = default)
    {
        // ObtenerAsync filtra por usuario: nadie borra bloques ajenos (BOLA).
        var h = await _repo.ObtenerAsync(id, usuarioId, ct)
                ?? throw new ReglaNegocioException("NO_ENCONTRADO", "El bloque no existe.");
        _repo.Eliminar(h);
        await _repo.GuardarCambiosAsync(ct);
        await _puentes.RecalcularAsync(usuarioId, ct);
    }

    // ---------- helpers ----------
    private static (TimeOnly, TimeOnly) ParsearYValidar(GuardarHorarioRequest req)
    {
        // Se acepta HH:mm y HH:mm:ss; se responde siempre HH:mm:ss (contrato).
        if (!TimeOnly.TryParseExact(req.HoraInicio, Formatos, out var ini) ||
            !TimeOnly.TryParseExact(req.HoraFin, Formatos, out var fin))
            throw new ReglaNegocioException("VALIDACION", "Las horas deben tener formato HH:mm o HH:mm:ss.");

        var error = ReglasHorario.ValidarBloque(req.Materia, req.DiaSemana, ini, fin);
        if (error is not null) throw new ReglaNegocioException("VALIDACION", error);
        return (ini, fin);
    }

    private static bool EsMismoBloque(int diaA, TimeOnly iniA, string materiaA, int diaB, TimeOnly iniB, string materiaB)
        => diaA == diaB && iniA == iniB && string.Equals(materiaA.Trim(), materiaB.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string MensajeDuplicado(string materia) => $"Ya registraste «{materia}» a esa hora ese día.";

    /// <summary>Dos clases distintas pueden empezar a la misma hora; lo que no se repite es la misma materia el mismo día y hora.</summary>
    private async Task VerificarDuplicadoAsync(int usuarioId, int dia, TimeOnly ini, string materia, int? ignorarId, CancellationToken ct)
    {
        var existentes = await _repo.ListarPorUsuarioAsync(usuarioId, ct);
        if (existentes.Any(b => b.Id != ignorarId && EsMismoBloque(b.DiaSemana, b.HoraInicio, b.Materia, dia, ini, materia)))
            throw new ReglaNegocioException("DUPLICADO", MensajeDuplicado(materia.Trim()));
    }

    private static string? LimpiarAula(string? a) => string.IsNullOrWhiteSpace(a) ? null : a.Trim();

    private static HorarioDto ADto(Horario h, IEnumerable<Horario> todos)
    {
        var coincide = ReglasHorario.Coincidencias(h, todos);
        var advertencias = ReglasHorario.Choques(h, todos)
            .Select(o => $"Coincide con «{o.Materia}» ({o.HoraInicio:HH\\:mm}-{o.HoraFin:HH\\:mm}).")
            .ToList();
        return new HorarioDto(
            h.Id, h.Materia, h.DiaSemana, h.HoraInicio.ToString("HH:mm:ss"), h.HoraFin.ToString("HH:mm:ss"), h.Aula, h.Origen,
            h.Asiste, h.AutorizacionChoque,
            coincide.Select(o => new CoincidenciaDto(o.Id, o.Materia, o.HoraInicio.ToString("HH:mm:ss"), o.HoraFin.ToString("HH:mm:ss"), o.Asiste)).ToList(),
            advertencias);
    }
}
