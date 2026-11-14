using System.Text.Json;
using Breaku.Application.Horarios;
using Breaku.Application.Puentes;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Matching;

public class MatchingService
{
    public const string EstadoOk = "OK";
    public const string EstadoSinResultados = "SIN_RESULTADOS";
    public const string EstadoSinZona = "SIN_ZONA";
    public const string EstadoNoVisible = "NO_VISIBLE";
    /// <summary>HU12: sin el test de afinidad no se muestra a nadie.</summary>
    public const string EstadoSinTest = "SIN_TEST";
    public const string MensajeSinTest =
        "Haz el test de afinidad (unos 2 minutos) para ver con quién coincides, además de en tiempo libre.";
    public const string MensajeSinCoincidencias =
        "Hay gente libre a esa hora, pero nadie con quien coincidas también en el test. Prueba con otro de tus puentes.";

    private readonly IMatchingRepository _repo;
    private readonly IPuenteRepository _puentes;
    private readonly IAfinidadRepository _afinidad;
    private readonly IConexionRepository _conexiones;

    public MatchingService(IMatchingRepository repo, IPuenteRepository puentes, IAfinidadRepository afinidad, IConexionRepository conexiones)
    {
        _repo = repo;
        _puentes = puentes;
        _afinidad = afinidad;
        _conexiones = conexiones;
    }

    public async Task<ConfiguracionMatchingDto> ObtenerConfiguracionAsync(int usuarioId, CancellationToken ct = default)
        => AConfiguracion(await ObtenerUsuarioAsync(usuarioId, ct));

    public async Task<ConfiguracionMatchingDto> ActualizarConfiguracionAsync(
        int usuarioId, ActualizarConfiguracionMatchingRequest req, CancellationToken ct = default)
    {
        if (req.Visible is null) throw new ReglaNegocioException("VALIDACION", "Indica si quieres aparecer (visible: true o false).");

        var usuario = await ObtenerUsuarioAsync(usuarioId, ct);
        if (usuario.VisibleMatching == req.Visible) return AConfiguracion(usuario);

        var antes = Valores(usuario);
        usuario.VisibleMatching = req.Visible.Value;
        usuario.FechaVisibleMatching = req.Visible.Value ? DateTime.UtcNow : null;
        usuario.UpdatedAt = DateTime.UtcNow;
        usuario.UpdatedBy = usuarioId;

        await _repo.GuardarConHistorialAsync(usuarioId, antes, Valores(usuario), ct);
        return AConfiguracion(usuario);
    }

    public async Task<ResultadoMatchingDto> BuscarAsync(int usuarioId, int puenteId, CancellationToken ct = default)
    {
        var misPuentes = await _puentes.ListarPorUsuarioAsync(usuarioId, ct);
        var puente = misPuentes.FirstOrDefault(p => p.Id == puenteId)
                     ?? throw new ReglaNegocioException("NO_ENCONTRADO", "El puente no existe.");
        var usuario = await ObtenerUsuarioAsync(usuarioId, ct);

        var mia = AFranja(puente);
        ResultadoMatchingDto Vacio(string estado, string mensaje) => new(estado, mensaje, APuenteDto(puente), mia.Minutos, []);

        if (usuario.ZonaActualId is not int zonaId || usuario.ZonaActual is not { Activa: true })
            return Vacio(EstadoSinZona, ReglasMatching.MensajeSinZona);
        if (!usuario.VisibleMatching)
            return Vacio(EstadoNoVisible, ReglasMatching.MensajeNoVisible);

        var miTest = await _afinidad.ObtenerTestAsync(usuarioId, ct);
        if (miTest is null || miTest.Version != ReglasAfinidad.Version)
            return Vacio(EstadoSinTest, MensajeSinTest);

        var candidatos = await _repo.ListarCandidatosAsync(usuarioId, zonaId, puente, ct);
        var porId = candidatos.ToDictionary(c => c.UsuarioId);
        var puentesDeOtros = candidatos.SelectMany(c => c.Puentes.Select(p => new ReglasMatching.PuenteDe(c.UsuarioId, AFranja(p))));

        // 1) Coinciden en tiempo (30 min o más el mismo día). 2) Coinciden en el test. Si falla una, no se muestran.
        var enTiempo = ReglasMatching.Calcular(mia, misPuentes.Select(AFranja).ToList(), puentesDeOtros);
        if (enTiempo.Count == 0) return Vacio(EstadoSinResultados, ReglasMatching.MensajeSinResultados);

        var tests = await _afinidad.ObtenerTestsAsync(enTiempo.Select(c => c.UsuarioId).ToList(), ReglasAfinidad.Version, ct);
        var conexiones = (await _conexiones.ListarDeUsuarioAsync(usuarioId, ct))
            .ToDictionary(c => c.SolicitanteId == usuarioId ? c.DestinatarioId : c.SolicitanteId);

        var companeros = enTiempo
            .Where(c => tests.ContainsKey(c.UsuarioId))
            .Select(c => (Tiempo: c, Test: ReglasAfinidad.Comparar(miTest.Respuestas, tests[c.UsuarioId])))
            .Where(x => ReglasAfinidad.Coinciden(x.Test))
            .Select(x => ACompanero(porId[x.Tiempo.UsuarioId], x.Tiempo, x.Test,
                conexiones.TryGetValue(x.Tiempo.UsuarioId, out var con) ? AResumen(con, usuarioId) : null))
            .OrderByDescending(c => c.Compatibilidad)
            .ThenByDescending(c => c.MinutosEnComun)
            .ThenBy(c => c.UsuarioId)
            .Take(ReglasMatching.MaximoCompaneros)
            .ToList();

        return companeros.Count == 0
            ? Vacio(EstadoSinResultados, MensajeSinCoincidencias)
            : new ResultadoMatchingDto(EstadoOk, null, APuenteDto(puente), mia.Minutos, companeros);
    }

    /// <summary>Un saludo rechazado se le muestra a quien lo envió como pendiente: el «no» es silencioso.</summary>
    internal static ConexionResumenDto AResumen(ConexionGuardada c, int usuarioId)
    {
        var mia = c.SolicitanteId == usuarioId;
        var estado = mia && c.Estado == ReglasConexion.Rechazada ? ReglasConexion.Pendiente : c.Estado;
        return new ConexionResumenDto(c.Id, estado, mia);
    }

    private async Task<Usuario> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct) =>
        await _repo.ObtenerUsuarioAsync(usuarioId, ct)
        ?? throw new ReglaNegocioException("NO_ENCONTRADO", "No encontramos tu cuenta.");

    private static ReglasMatching.Franja AFranja(Puente p) => new(p.DiaSemana, p.HoraInicio, p.HoraFin);

    private static string Valores(Usuario u) =>
        JsonSerializer.Serialize(new { visible_matching = u.VisibleMatching, fecha_visible_matching = u.FechaVisibleMatching });

    private static ConfiguracionMatchingDto AConfiguracion(Usuario u) => new(u.VisibleMatching, u.FechaVisibleMatching);

    private static PuenteDto APuenteDto(Puente p) =>
        new(p.Id, p.DiaSemana, p.HoraInicio.ToString("HH:mm:ss"), p.HoraFin.ToString("HH:mm:ss"), p.Tipo);

    private static CompaneroCompatibleDto ACompanero(
        CompaneroCandidato c, ReglasMatching.Compatibilidad m, ReglasAfinidad.Resultado test, ConexionResumenDto? conexion) =>
        new(c.UsuarioId, ReglasDescubrir.NombreCorto(c.Nombre), c.Carrera, c.Semestre,
            m.EnComun.Inicio.ToString("HH:mm:ss"), m.EnComun.Fin.ToString("HH:mm:ss"),
            m.MinutosEnComun, m.Porcentaje, m.FranjasCompartidas,
            test.Afinidad, ReglasAfinidad.Compatibilidad(test.Afinidad, m.Porcentaje), test.EnComun.ToList(), conexion);
}
