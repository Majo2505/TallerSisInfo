using Breaku.Application.TiempoReal;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Sanciones;

/// <summary>Lo que hay que avisar en vivo, cuando la transacción ya se confirmó.</summary>
public sealed record ResultadoPurga(
    IReadOnlyList<int> UsuariosAAvisar,
    IReadOnlyList<(int ComunidadId, int Confirmados)> ComunidadesActualizadas);

/// <summary>
/// HU28/HU29: la rutina de purga compartida. Borra lo del usuario de forma explícita, en una sola transacción y en el orden
/// de <see cref="ReglasPurga.Pasos"/>. La BAJA (HU28) deja la fila de <c>usuario</c> anonimizada y conserva sus sanciones;
/// la eliminación de cuenta (HU29) borra también las sanciones y la fila.
/// </summary>
public class PurgaUsuarioService
{
    private readonly IPurgaRepository _repo;
    private readonly ITransaccion _transaccion;
    private readonly ITiempoReal _tiempoReal;
    private readonly TimeProvider _reloj;

    public PurgaUsuarioService(IPurgaRepository repo, ITransaccion transaccion, ITiempoReal tiempoReal, TimeProvider reloj)
    {
        _repo = repo;
        _transaccion = transaccion;
        _tiempoReal = tiempoReal;
        _reloj = reloj;
    }

    /// <summary>Si se llama dentro de otra transacción (la de la baja), se une a ella.</summary>
    public Task<ResultadoPurga> PurgarAsync(int usuarioId, bool eliminarCuenta, CancellationToken ct = default)
        => _transaccion.EjecutarAsync(async () =>
        {
            var ahora = _reloj.GetUtcNow().UtcDateTime;
            var aAvisar = new List<int>();
            var actualizadas = new List<(int ComunidadId, int Confirmados)>();

            foreach (var paso in ReglasPurga.Pasos(eliminarCuenta))
            {
                switch (paso)
                {
                    case ReglasPurga.Paso.ComunidadesCreadas:
                        await PurgarComunidadesCreadasAsync(usuarioId, ahora, aAvisar, actualizadas, ct);
                        break;
                    case ReglasPurga.Paso.Participaciones:
                        actualizadas.AddRange(await _repo.EliminarParticipacionesAsync(usuarioId, ct));
                        break;
                    case ReglasPurga.Paso.Notificaciones: await _repo.EliminarNotificacionesAsync(usuarioId, ct); break;
                    case ReglasPurga.Paso.Eventos: await _repo.EliminarEventosAsync(usuarioId, ct); break;
                    case ReglasPurga.Paso.Reportes: await _repo.EliminarReportesAsync(usuarioId, ct); break;
                    case ReglasPurga.Paso.Recomendaciones: await _repo.EliminarRecomendacionesAsync(usuarioId, ct); break;
                    case ReglasPurga.Paso.LugaresRecuperados: await _repo.EliminarLugaresRecuperadosAsync(usuarioId, ct); break;
                    case ReglasPurga.Paso.LogsAuditoria: await _repo.EliminarLogsAuditoriaAsync(usuarioId, ct); break;
                    case ReglasPurga.Paso.Puentes: await _repo.EliminarPuentesAsync(usuarioId, ct); break;
                    case ReglasPurga.Paso.Horarios: await _repo.EliminarHorariosAsync(usuarioId, ct); break;
                    case ReglasPurga.Paso.HistorialCambio: await _repo.EliminarHistorialAsync(usuarioId, ct); break;
                    case ReglasPurga.Paso.Sanciones: await _repo.EliminarSancionesAsync(usuarioId, ct); break;
                    case ReglasPurga.Paso.Usuario:
                        if (eliminarCuenta) await _repo.EliminarUsuarioAsync(usuarioId, ct);
                        else await _repo.AnonimizarUsuarioAsync(usuarioId, ReglasSancion.HashInutilizable(), ahora, ct);
                        break;
                }
            }

            return new ResultadoPurga(aAvisar.Where(id => id != usuarioId).Distinct().ToList(), actualizadas);
        }, ct);

    /// <summary>Avisa en vivo (campanita y cupo de las comunidades). Un fallo acá no deshace nada: ya está confirmado.</summary>
    public async Task AvisarAsync(ResultadoPurga resultado, CancellationToken ct = default)
    {
        await _tiempoReal.AvisosCambiaronAsync(resultado.UsuariosAAvisar, ct);
        foreach (var (comunidadId, confirmados) in resultado.ComunidadesActualizadas)
            await _tiempoReal.ComunidadActualizadaAsync(comunidadId, ComunidadEfimera.EstadoActiva, confirmados, ct);
    }

    private async Task PurgarComunidadesCreadasAsync(int usuarioId, DateTime ahora, List<int> aAvisar,
        List<(int ComunidadId, int Confirmados)> actualizadas, CancellationToken ct)
    {
        foreach (var comunidad in await _repo.ListarComunidadesCreadasAsync(usuarioId, ct))
        {
            var decision = ReglasPurga.DecidirComunidad(usuarioId, comunidad);
            aAvisar.AddRange(comunidad.Participantes.Select(p => p.UsuarioId));

            if (decision.Destino == ReglasPurga.Destino.CambiarCreador)
            {
                await _repo.CambiarCreadorAsync(comunidad.Id, decision.NuevoCreadorId!.Value, ahora, ct);
                actualizadas.Add((comunidad.Id, decision.ConfirmadosRestantes));
            }
            else
            {
                await _repo.EliminarComunidadAsync(comunidad.Id, ct);
            }
        }
    }
}
