using Breaku.Domain;
using Breaku.Application.Horarios;

namespace Breaku.Application.Comunidades;

public sealed class ComunidadService(IComunidadRepository repo)
{
    public async Task<List<ComunidadDto>> ListarAsync(CancellationToken ct = default)
    {
        await repo.ArchivarVencidasAsync(DateTime.UtcNow, ct);
        var comunidades = await repo.ListarActivasAsync(DateTime.UtcNow, ct);
        return (await Task.WhenAll(comunidades.Select(async c => ADto(c, await repo.ContarParticipantesAsync(c.Id, ct))))).ToList();
    }

    public async Task<ComunidadDto> CrearAsync(int usuarioId, CrearComunidadRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Texto) || string.IsNullOrWhiteSpace(req.Actividad))
            throw new ReglaNegocioException("VALIDACION", "Indica qué actividad quieres realizar.");
        if (req.Fin <= req.Inicio || req.Fin - req.Inicio > TimeSpan.FromHours(8))
            throw new ReglaNegocioException("VALIDACION", "El horario de la comunidad no es válido.");
        if (req.MaxParticipantes is < 2 or > 30)
            throw new ReglaNegocioException("VALIDACION", "El grupo debe tener entre 2 y 30 participantes.");

        var comunidad = new ComunidadEfimera
        {
            CreadorId = usuarioId, TextoOriginal = req.Texto.Trim(), Actividad = req.Actividad.Trim(),
            Inicio = DateTime.SpecifyKind(req.Inicio, DateTimeKind.Utc), Fin = DateTime.SpecifyKind(req.Fin, DateTimeKind.Utc),
            MaxParticipantes = req.MaxParticipantes, LugarId = req.LugarId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var candidatos = await repo.ListarCandidatosAsync(usuarioId, comunidad.Inicio, comunidad.Fin, comunidad.MaxParticipantes, ct);
        var creada = await repo.CrearAsync(comunidad, [usuarioId, .. candidatos], ct);
        return ADto(creada, 1);
    }

    public async Task<ParticipacionDto> ResponderAsync(int usuarioId, int comunidadId, bool confirmar, CancellationToken ct = default)
    {
        var actualizado = await repo.ResponderAsync(comunidadId, usuarioId, confirmar ? "CONFIRMADO" : "RECHAZADO", ct);
        if (!actualizado) throw new ReglaNegocioException("NO_ENCONTRADO", "No tienes una invitación pendiente para esta comunidad.");
        return new ParticipacionDto(comunidadId, confirmar ? "CONFIRMADO" : "RECHAZADO");
    }

    private static ComunidadDto ADto(ComunidadEfimera c, int participantes)
        => new(c.Id, c.Actividad, c.TextoOriginal, c.Inicio, c.Fin, c.MaxParticipantes, c.Estado, participantes, null);
}
