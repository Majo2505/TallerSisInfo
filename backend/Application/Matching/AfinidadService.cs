using Breaku.Application.Horarios;
using Breaku.Domain.Rules;

namespace Breaku.Application.Matching;

/// <summary>HU12: el test de afinidad. Las preguntas salen de <see cref="ReglasAfinidad"/>; el frontend no las repite.</summary>
public class AfinidadService
{
    private readonly IAfinidadRepository _repo;
    public AfinidadService(IAfinidadRepository repo) => _repo = repo;

    public async Task<TestAfinidadDto> ObtenerAsync(int usuarioId, CancellationToken ct = default)
        => ATest(await _repo.ObtenerTestAsync(usuarioId, ct));

    public async Task<TestAfinidadDto> GuardarAsync(int usuarioId, GuardarTestRequest req, CancellationToken ct = default)
    {
        Dictionary<string, List<string>> respuestas;
        try
        {
            respuestas = ReglasAfinidad.Validar(req.Respuestas);
        }
        catch (ArgumentException ex)
        {
            throw new ReglaNegocioException("VALIDACION", ex.Message);
        }

        await _repo.GuardarTestAsync(usuarioId, ReglasAfinidad.Version, respuestas, DateTime.UtcNow, ct);
        return ATest(await _repo.ObtenerTestAsync(usuarioId, ct));
    }

    private static TestAfinidadDto ATest(TestGuardado? test)
    {
        // Un test de una versión vieja no cuenta: se pide de nuevo (con las preguntas nuevas).
        var vigente = test is not null && test.Version == ReglasAfinidad.Version;
        return new TestAfinidadDto(
            ReglasAfinidad.Version,
            ReglasAfinidad.MinutosEstimados,
            ReglasAfinidad.Preguntas
                .Select(p => new PreguntaTestDto(p.Id, p.Bloque, p.Texto, p.Tipo, p.Maximo, p.Opcional,
                    p.Opciones.Select(o => new OpcionTestDto(o.Id, o.Texto)).ToList()))
                .ToList(),
            vigente,
            vigente ? test!.Respuestas : null,
            vigente ? test!.ActualizadoEn : null);
    }
}
