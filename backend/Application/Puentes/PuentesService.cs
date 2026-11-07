using Breaku.Application.Horarios;
using Breaku.Application.Notificaciones;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Puentes;

public class PuenteService
{
    private readonly IHorarioRepository _horarios;
    private readonly IPuenteRepository _puentes;
    private readonly NotificacionService _notificaciones;

    public PuenteService(IHorarioRepository horarios, IPuenteRepository puentes, NotificacionService notificaciones)
    {
        _horarios = horarios;
        _puentes = puentes;
        _notificaciones = notificaciones;
    }

    public async Task<List<PuenteDto>> ListarAsync(int usuarioId, CancellationToken ct = default)
    {
        var puentes = await _puentes.ListarPorUsuarioAsync(usuarioId, ct);
        return puentes.OrderBy(p => p.DiaSemana).ThenBy(p => p.HoraInicio).Select(ADto).ToList();
    }

    public async Task RecalcularAsync(int usuarioId, CancellationToken ct = default)
    {
        var bloques = await _horarios.ListarPorUsuarioAsync(usuarioId, ct);
        var puentes = DetectarPuentes(usuarioId, bloques);
        await _puentes.ReemplazarAsync(usuarioId, puentes, ct);
        await _notificaciones.DesactivarSinPuenteAsync(usuarioId, puentes, ct);
    }

    public static List<Puente> DetectarPuentes(int usuarioId, IEnumerable<Horario> bloques)
    {
        var puentes = new List<Puente>();

        foreach (var dia in bloques.Where(b => b.Asiste).GroupBy(b => b.DiaSemana))
        {
            var ocupado = ReglasHorario.UnirIntervalos(dia.Select(b => new ReglasHorario.Intervalo(b.HoraInicio, b.HoraFin)));

            for (var i = 1; i < ocupado.Count; i++)
            {
                if (!ReglasPuente.EsPuente(ocupado[i - 1].Fin, ocupado[i].Inicio)) continue;

                puentes.Add(new Puente
                {
                    UsuarioId = usuarioId,
                    DiaSemana = (short)dia.Key,
                    HoraInicio = ocupado[i - 1].Fin,
                    HoraFin = ocupado[i].Inicio
                });
            }

            var finUltimaClase = ocupado[^1].Fin;
            if (ReglasPuente.QuedaTiempoDespuesDeClases(finUltimaClase))
            {
                puentes.Add(new Puente
                {
                    UsuarioId = usuarioId,
                    DiaSemana = (short)dia.Key,
                    HoraInicio = finUltimaClase,
                    HoraFin = ReglasHorario.FinMasTarde,
                    Tipo = Puente.TipoDespuesDeClases
                });
            }
        }

        return puentes;
    }

    private static PuenteDto ADto(Puente p) =>
        new(p.Id, p.DiaSemana, p.HoraInicio.ToString("HH:mm:ss"), p.HoraFin.ToString("HH:mm:ss"), p.Tipo);
}