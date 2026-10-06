using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Horarios;

/// <summary>Casos de uso de HU2: alta, edición y eliminación de bloques, con validación de solapamientos y persistencia por usuario.</summary>
public class HorarioService
{
    private readonly IHorarioRepository _repo;
    public HorarioService(IHorarioRepository repo) => _repo = repo;

    public async Task<List<HorarioDto>> ListarAsync(int usuarioId, CancellationToken ct = default)
    {
        var bloques = await _repo.ListarPorUsuarioAsync(usuarioId, ct);
        return bloques.OrderBy(b => b.DiaSemana).ThenBy(b => b.HoraInicio).Select(ADto).ToList();
    }

    public async Task<HorarioDto> CrearAsync(int usuarioId, GuardarHorarioRequest req, CancellationToken ct = default)
    {
        var (ini, fin) = ParsearYValidar(req);
        await VerificarSolapamientoAsync(usuarioId, req.DiaSemana, ini, fin, null, ct);

        var ahora = DateTime.UtcNow;
        var h = new Horario
        {
            UsuarioId = usuarioId, // siempre del JWT, nunca del body
            Materia = req.Materia.Trim(), DiaSemana = req.DiaSemana,
            HoraInicio = ini, HoraFin = fin, Aula = LimpiarAula(req.Aula),
            Origen = "MANUAL", CreatedAt = ahora, UpdatedAt = ahora
        };
        await _repo.AgregarAsync(h, ct);
        await _repo.GuardarCambiosAsync(ct);
        return ADto(h);
    }

    public async Task<HorarioDto> EditarAsync(int usuarioId, int id, GuardarHorarioRequest req, CancellationToken ct = default)
    {
        var h = await _repo.ObtenerAsync(id, usuarioId, ct)
                ?? throw new ReglaNegocioException("NO_ENCONTRADO", "El bloque no existe.");
        var (ini, fin) = ParsearYValidar(req);
        await VerificarSolapamientoAsync(usuarioId, req.DiaSemana, ini, fin, id, ct);

        h.Materia = req.Materia.Trim(); h.DiaSemana = req.DiaSemana;
        h.HoraInicio = ini; h.HoraFin = fin; h.Aula = LimpiarAula(req.Aula);
        h.UpdatedAt = DateTime.UtcNow;
        await _repo.GuardarCambiosAsync(ct);
        return ADto(h);
    }

    public async Task EliminarAsync(int usuarioId, int id, CancellationToken ct = default)
    {
        // ObtenerAsync filtra por usuario: nadie borra bloques ajenos (BOLA).
        var h = await _repo.ObtenerAsync(id, usuarioId, ct)
                ?? throw new ReglaNegocioException("NO_ENCONTRADO", "El bloque no existe.");
        _repo.Eliminar(h);
        await _repo.GuardarCambiosAsync(ct);
    }

    // ---------- helpers ----------
    private static (TimeOnly, TimeOnly) ParsearYValidar(GuardarHorarioRequest req)
    {
        if (!TimeOnly.TryParseExact(req.HoraInicio, "HH:mm", out var ini) ||
            !TimeOnly.TryParseExact(req.HoraFin, "HH:mm", out var fin))
            throw new ReglaNegocioException("VALIDACION", "Las horas deben tener formato HH:mm.");

        var error = ReglasHorario.ValidarBloque(req.Materia, req.DiaSemana, ini, fin);
        if (error is not null) throw new ReglaNegocioException("VALIDACION", error);
        return (ini, fin);
    }

    private async Task VerificarSolapamientoAsync(int usuarioId, int dia, TimeOnly ini, TimeOnly fin, int? ignorarId, CancellationToken ct)
    {
        var existentes = await _repo.ListarPorUsuarioAsync(usuarioId, ct);
        var choque = existentes.FirstOrDefault(b =>
            b.Id != ignorarId && b.DiaSemana == dia && ReglasHorario.Solapa(ini, fin, b.HoraInicio, b.HoraFin));
        if (choque is not null)
            throw new ReglaNegocioException("SOLAPAMIENTO",
                $"Se solapa con «{choque.Materia}» ({choque.HoraInicio:HH\\:mm}-{choque.HoraFin:HH\\:mm}).");
    }

    private static string? LimpiarAula(string? a) => string.IsNullOrWhiteSpace(a) ? null : a.Trim();

    private static HorarioDto ADto(Horario h) =>
        new(h.Id, h.Materia, h.DiaSemana, h.HoraInicio.ToString("HH:mm"), h.HoraFin.ToString("HH:mm"), h.Aula, h.Origen);
}
