using Breaku.Application.Horarios;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Puentes;

/// <summary>Casos de uso de HU3: consultar los puentes del usuario y recalcularlos a partir de su horario.</summary>
public class PuenteService
{
    private readonly IHorarioRepository _horarios;
    private readonly IPuenteRepository _puentes;

    public PuenteService(IHorarioRepository horarios, IPuenteRepository puentes)
    {
        _horarios = horarios;
        _puentes = puentes;
    }

    public async Task<List<PuenteDto>> ListarAsync(int usuarioId, CancellationToken ct = default)
    {
        var puentes = await _puentes.ListarPorUsuarioAsync(usuarioId, ct);
        return puentes.OrderBy(p => p.DiaSemana).ThenBy(p => p.HoraInicio).Select(ADto).ToList();
    }

    /// <summary>
    /// Recalcula y reemplaza los puentes del usuario. Debe llamarse DESPUÉS de guardar
    /// cualquier cambio de horario (POST, PUT y DELETE de /api/horarios). No hay endpoint público.
    /// </summary>
    public async Task RecalcularAsync(int usuarioId, CancellationToken ct = default)
    {
        var bloques = await _horarios.ListarPorUsuarioAsync(usuarioId, ct);
        var puentes = DetectarPuentes(usuarioId, bloques);
        await _puentes.ReemplazarAsync(usuarioId, puentes, ct);
    }

    /// <summary>
    /// Recorre las clases de cada día y arma un puente por cada hueco que cumple la regla.
    /// Solo cuentan huecos ENTRE clases del mismo día (antes de la primera y después de la última no).
    /// Es estática y no toca la base de datos, así que se puede probar sola.
    /// </summary>
    public static List<Puente> DetectarPuentes(int usuarioId, IEnumerable<Horario> bloques)
    {
        var puentes = new List<Puente>();

        foreach (var dia in bloques.GroupBy(b => b.DiaSemana))
        {
            var ordenados = dia.OrderBy(b => b.HoraInicio).ThenBy(b => b.HoraFin).ToList();

            // Se compara contra el fin MÁS TARDÍO visto hasta ahora (no solo contra la clase anterior),
            // para no inventar un hueco si hubiera datos solapados.
            var finMasTardio = ordenados[0].HoraFin;

            foreach (var actual in ordenados.Skip(1))
            {
                if (ReglasPuente.EsPuente(finMasTardio, actual.HoraInicio))
                {
                    puentes.Add(new Puente
                    {
                        UsuarioId = usuarioId,
                        DiaSemana = (short)dia.Key,
                        HoraInicio = finMasTardio,
                        HoraFin = actual.HoraInicio
                    });
                }

                if (actual.HoraFin > finMasTardio) finMasTardio = actual.HoraFin;
            }
        }

        return puentes;
    }

    private static PuenteDto ADto(Puente p) =>
        new(p.Id, p.DiaSemana, p.HoraInicio.ToString("HH:mm:ss"), p.HoraFin.ToString("HH:mm:ss"));
}