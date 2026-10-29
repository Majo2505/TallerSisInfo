using Breaku.Application.Horarios;
using Breaku.Application.Notificaciones;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Puentes;

/// <summary>Casos de uso de HU3: consultar los puentes del usuario y recalcularlos a partir de su horario.</summary>
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

    /// <summary>
    /// Recalcula y reemplaza los puentes del usuario. Debe llamarse DESPUÉS de guardar
    /// cualquier cambio de horario (POST, PUT y DELETE de /api/horarios). No hay endpoint público.
    /// Además desactiva los avisos de puentes que dejaron de existir (HU4).
    /// </summary>
    public async Task RecalcularAsync(int usuarioId, CancellationToken ct = default)
    {
        var bloques = await _horarios.ListarPorUsuarioAsync(usuarioId, ct);
        var puentes = DetectarPuentes(usuarioId, bloques);
        await _puentes.ReemplazarAsync(usuarioId, puentes, ct);
        await _notificaciones.DesactivarSinPuenteAsync(usuarioId, puentes, ct);
    }

    /// <summary>
    /// Arma un puente por cada hueco que cumple la regla entre los tramos ocupados de un mismo día.
    /// Solo cuentan los bloques a los que el estudiante asiste, y el tiempo ocupado es la UNIÓN de sus intervalos:
    /// si dos clases coinciden (o una contiene a otra) no se generan huecos negativos ni repetidos.
    /// Solo cuentan huecos ENTRE clases del mismo día (antes de la primera y después de la última no).
    /// Es estática y no toca la base de datos, así que se puede probar sola.
    /// </summary>
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
        }

        return puentes;
    }

    private static PuenteDto ADto(Puente p) =>
        new(p.Id, p.DiaSemana, p.HoraInicio.ToString("HH:mm:ss"), p.HoraFin.ToString("HH:mm:ss"));
}