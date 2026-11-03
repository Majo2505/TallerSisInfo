using Breaku.Domain;
using Breaku.Domain.Rules;
using Breaku.Application.Puentes;

namespace Breaku.Application.Horarios;

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
            UsuarioId = usuarioId,
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
                    UsuarioId = usuarioId,
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

    public async Task<HorarioDto> CambiarAsistenciaAsync(int usuarioId, int id, bool asiste, bool? autorizacionCarrera = null, CancellationToken ct = default)
    {
        var resultado = await CambiarAsistenciasAsync(usuarioId, [new AsistenciaBloqueRequest(id, asiste, autorizacionCarrera)], ct);
        return resultado.Single();
    }

    public async Task<List<HorarioDto>> CambiarAsistenciasAsync(int usuarioId, List<AsistenciaBloqueRequest> cambios, CancellationToken ct = default)
    {
        if (cambios is null || cambios.Count == 0)
            throw new ReglaNegocioException("VALIDACION", "Envía al menos un bloque.");
        if (cambios.Count > MaximoBloquesPorLote)
            throw new ReglaNegocioException("VALIDACION", $"Se pueden cambiar hasta {MaximoBloquesPorLote} bloques a la vez.");

        var propios = (await _repo.ListarPorUsuarioAsync(usuarioId, ct)).ToDictionary(b => b.Id);
        if (cambios.Any(c => !propios.ContainsKey(c.Id))) throw new ReglaNegocioException("NO_ENCONTRADO", "El bloque no existe.");

        var ahora = DateTime.UtcNow;
        foreach (var c in cambios)
        {
            var h = propios[c.Id];
            h.Asiste = c.Asiste;
            if (c.AutorizacionCarrera is not null) h.AutorizacionChoque = c.AutorizacionCarrera.Value;
            h.UpdatedAt = ahora;
        }

        await _repo.GuardarCambiosAsync(ct);
        await _puentes.RecalcularAsync(usuarioId, ct);
        var todos = await _repo.ListarPorUsuarioAsync(usuarioId, ct);
        return cambios.Select(c => ADto(todos.First(b => b.Id == c.Id), todos)).ToList();
    }

    public async Task EliminarAsync(int usuarioId, int id, CancellationToken ct = default)
    {
        var h = await _repo.ObtenerAsync(id, usuarioId, ct)
                ?? throw new ReglaNegocioException("NO_ENCONTRADO", "El bloque no existe.");
        _repo.Eliminar(h);
        await _repo.GuardarCambiosAsync(ct);
        await _puentes.RecalcularAsync(usuarioId, ct);
    }

    private static (TimeOnly, TimeOnly) ParsearYValidar(GuardarHorarioRequest req)
    {
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
