using Breaku.Application.Horarios;
using Breaku.Application.TiempoReal;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Sanciones;

/// <summary>
/// HU28: el administrador ve a los estudiantes y los sanciona (ADVERTENCIA, SUSPENSION o BAJA). Orquesta: las reglas
/// están en <see cref="ReglasSancion"/> y <see cref="ReglasPurga"/>.
/// </summary>
public class SancionService
{
    public const int LimitePorDefecto = 100;
    public const int LimiteMaximo = 200;

    private readonly ISancionRepository _repo;
    private readonly PurgaUsuarioService _purga;
    private readonly ITransaccion _transaccion;
    private readonly ITiempoReal _tiempoReal;
    private readonly IValidadorSesion _validador;
    private readonly TimeProvider _reloj;

    public SancionService(ISancionRepository repo, PurgaUsuarioService purga, ITransaccion transaccion, ITiempoReal tiempoReal,
        IValidadorSesion validador, TimeProvider reloj)
    {
        _repo = repo;
        _purga = purga;
        _transaccion = transaccion;
        _tiempoReal = tiempoReal;
        _validador = validador;
        _reloj = reloj;
    }

    public async Task<List<UsuarioListaDto>> ListarAsync(string? buscar, int? limite, CancellationToken ct = default)
    {
        var ahora = _reloj.GetUtcNow().UtcDateTime;
        var usuarios = await _repo.ListarEstudiantesAsync(buscar?.Trim(), Math.Clamp(limite ?? LimitePorDefecto, 1, LimiteMaximo), ct);
        if (usuarios.Count == 0) return [];

        var registros = (await _repo.ListarRegistrosAsync(usuarios.Select(u => u.Id).ToList(), ct))
            .ToLookup(r => r.UsuarioId, r => r.Registro);

        return usuarios.Select(u =>
        {
            var propios = registros[u.Id].ToList();
            return new UsuarioListaDto(u.Id, u.Nombre, u.Correo, u.Carrera,
                ReglasSancion.EstadoVisible(ReglasSancion.BloqueoVigente(u.Activo, propios, ahora)),
                propios.Count, ReglasSancion.ContarAdvertencias(propios));
        }).ToList();
    }

    public async Task<UsuarioDetalleDto> ObtenerAsync(int usuarioId, CancellationToken ct = default)
    {
        var usuario = await _repo.ObtenerUsuarioAsync(usuarioId, ct);
        if (usuario is null || usuario.Rol == "ADMIN")
            throw new ReglaNegocioException(ReglasSancion.ErrorNoEncontrado, ReglasSancion.MensajeUsuarioNoExiste);

        var ahora = _reloj.GetUtcNow().UtcDateTime;
        var sanciones = await _repo.ListarSancionesAsync(usuarioId, ct);
        var registros = sanciones.Select(s => s.Registro).ToList();
        var bloqueo = ReglasSancion.BloqueoVigente(usuario.Activo, registros, ahora);
        var advertencias = ReglasSancion.ContarAdvertencias(registros);

        return new UsuarioDetalleDto(usuario.Id, usuario.Nombre, usuario.Correo, usuario.Carrera, usuario.Semestre,
            ReglasSancion.EstadoVisible(bloqueo), usuario.CreadoEn, advertencias, ReglasSancion.SiguienteSugerida(advertencias),
            ReglasSancion.AdvertenciasParaSuspension, ReglasSancion.DiasSuspensionPorDefecto,
            bloqueo is null ? null : new BloqueoDto(bloqueo.Tipo, bloqueo.Motivo, bloqueo.Hasta),
            sanciones.OrderByDescending(s => s.Registro.CreadoEn).ThenByDescending(s => s.Registro.Id)
                .Select(s => ADto(s, ahora)).ToList());
    }

    public async Task<SancionAplicadaDto> AplicarAsync(int adminId, int usuarioId, SancionarUsuarioDto req, CancellationToken ct = default)
    {
        var ahora = _reloj.GetUtcNow().UtcDateTime;

        var (valida, error) = ReglasSancion.Validar(req.Tipo, req.Motivo, req.Hasta, ahora);
        if (valida is null) throw new ReglaNegocioException(ReglasSancion.ErrorValidacion, error!);
        if (req.ReporteId is int reporteId && !await _repo.ReporteExisteAsync(reporteId, ct))
            throw new ReglaNegocioException(ReglasSancion.ErrorValidacion, ReglasSancion.MensajeReporteNoExiste);

        ResultadoPurga? purga = null;
        var aplicada = await _transaccion.EjecutarAsync(async () =>
        {
            await _repo.BloquearUsuarioAsync(usuarioId, ct);
            var usuario = await _repo.ObtenerUsuarioAsync(usuarioId, ct)
                          ?? throw new ReglaNegocioException(ReglasSancion.ErrorNoEncontrado, ReglasSancion.MensajeUsuarioNoExiste);

            var rechazo = ReglasSancion.ValidarObjetivo(adminId, usuarioId, usuario.Rol);
            if (rechazo is not null) throw new ReglaNegocioException(ReglasSancion.ErrorNoSancionable, rechazo);

            var previas = (await _repo.ListarSancionesAsync(usuarioId, ct)).Select(s => s.Registro).ToList();
            var estado = ReglasSancion.ValidarEstado(valida.Tipo, ReglasSancion.BloqueoVigente(usuario.Activo, previas, ahora));
            if (estado is { } rechazoEstado) throw new ReglaNegocioException(rechazoEstado.Codigo, rechazoEstado.Mensaje);

            var id = await _repo.AgregarAsync(usuarioId, valida, req.ReporteId, adminId, ahora, ct);

            // Solo la advertencia notifica: el suspendido y el dado de baja no pueden abrir sus avisos; ven el motivo al intentar entrar.
            if (valida.Tipo == Sancion.TipoAdvertencia)
                await _repo.AgregarAvisoAdvertenciaAsync(usuarioId, id, ReglasSancion.MensajeAdvertencia(valida.Motivo), ahora, ct);
            else if (valida.Tipo == Sancion.TipoBaja)
                purga = await _purga.PurgarAsync(usuarioId, eliminarCuenta: false, ct);

            return id;
        }, ct);

        // Ya confirmado: el caché deja de ser válido y se avisa en vivo.
        _validador.Invalidar(usuarioId);
        if (valida.Tipo == Sancion.TipoAdvertencia) await _tiempoReal.AvisosCambiaronAsync([usuarioId], ct);
        if (purga is not null) await _purga.AvisarAsync(purga, ct);

        var detalle = await ObtenerAsync(usuarioId, ct);
        var sancion = detalle.Sanciones.First(s => s.Id == aplicada);
        return new SancionAplicadaDto(sancion, detalle.Estado, detalle.Advertencias, detalle.SiguienteSancionSugerida);
    }

    public async Task<SancionDto> LevantarAsync(int adminId, int usuarioId, int sancionId, CancellationToken ct = default)
    {
        var ahora = _reloj.GetUtcNow().UtcDateTime;
        var sancion = await _repo.ObtenerSancionAsync(usuarioId, sancionId, ct)
                      ?? throw new ReglaNegocioException(ReglasSancion.ErrorNoEncontrado, ReglasSancion.MensajeSancionNoExiste);

        if (ReglasSancion.ValidarLevantar(sancion.Registro) is { } rechazo)
            throw new ReglaNegocioException(rechazo.Codigo, rechazo.Mensaje);
        if (!await _repo.LevantarAsync(sancionId, adminId, ahora, ct))
            throw new ReglaNegocioException(ReglasSancion.ErrorYaLevantada, ReglasSancion.MensajeYaLevantada);

        _validador.Invalidar(usuarioId);
        var levantada = await _repo.ObtenerSancionAsync(usuarioId, sancionId, ct);
        return ADto(levantada!, ahora);
    }

    private static SancionDto ADto(SancionDetalle s, DateTime ahora) =>
        new(s.Registro.Id, s.Registro.Tipo, s.Registro.Motivo, s.ReporteId, s.CreadoPor, s.Registro.CreadoEn, s.Registro.Hasta,
            s.Registro.LevantadaEn, s.LevantadaPor, ReglasSancion.EstaVigente(s.Registro, ahora));
}
