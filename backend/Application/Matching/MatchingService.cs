using System.Text.Json;
using Breaku.Application.Horarios;
using Breaku.Application.Puentes;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Matching;

/// <summary>
/// Casos de uso de HU12: encontrar compañeros de la misma sede con un puente compatible y ordenarlos por compatibilidad.
/// Mostrar cuándo está libre otro estudiante es un dato personal: solo aparece quien lo aceptó, y para ver a otros
/// hay que aceptar aparecer también. Cada cambio del consentimiento queda en historial_cambio.
/// </summary>
public class MatchingService
{
    public const string EstadoOk = "OK";
    public const string EstadoSinResultados = "SIN_RESULTADOS";
    public const string EstadoSinZona = "SIN_ZONA";
    public const string EstadoNoVisible = "NO_VISIBLE";

    private readonly IMatchingRepository _repo;
    private readonly IPuenteRepository _puentes;

    public MatchingService(IMatchingRepository repo, IPuenteRepository puentes)
    {
        _repo = repo;
        _puentes = puentes;
    }

    public async Task<ConfiguracionMatchingDto> ObtenerConfiguracionAsync(int usuarioId, CancellationToken ct = default)
        => AConfiguracion(await ObtenerUsuarioAsync(usuarioId, ct));

    public async Task<ConfiguracionMatchingDto> ActualizarConfiguracionAsync(
        int usuarioId, ActualizarConfiguracionMatchingRequest req, CancellationToken ct = default)
    {
        if (req.Visible is null) throw new ReglaNegocioException("VALIDACION", "Indica si quieres aparecer (visible: true o false).");

        var usuario = await ObtenerUsuarioAsync(usuarioId, ct);
        if (usuario.VisibleMatching == req.Visible) return AConfiguracion(usuario);   // sin cambios: no se escribe ni se audita

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

        var candidatos = await _repo.ListarCandidatosAsync(usuarioId, zonaId, puente, ct);
        var porId = candidatos.ToDictionary(c => c.UsuarioId);
        var puentesDeOtros = candidatos.SelectMany(c => c.Puentes.Select(p => new ReglasMatching.PuenteDe(c.UsuarioId, AFranja(p))));

        var companeros = ReglasMatching.Calcular(mia, misPuentes.Select(AFranja).ToList(), puentesDeOtros)
            .Take(ReglasMatching.MaximoCompaneros)
            .Select(c => ACompanero(porId[c.UsuarioId], c))
            .ToList();

        return companeros.Count == 0
            ? Vacio(EstadoSinResultados, ReglasMatching.MensajeSinResultados)
            : new ResultadoMatchingDto(EstadoOk, null, APuenteDto(puente), mia.Minutos, companeros);
    }

    private async Task<Usuario> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct) =>
        await _repo.ObtenerUsuarioAsync(usuarioId, ct)
        ?? throw new ReglaNegocioException("NO_ENCONTRADO", "No encontramos tu cuenta.");

    private static ReglasMatching.Franja AFranja(Puente p) => new(p.DiaSemana, p.HoraInicio, p.HoraFin);

    private static string Valores(Usuario u) =>
        JsonSerializer.Serialize(new { visible_matching = u.VisibleMatching, fecha_visible_matching = u.FechaVisibleMatching });

    private static ConfiguracionMatchingDto AConfiguracion(Usuario u) => new(u.VisibleMatching, u.FechaVisibleMatching);

    private static PuenteDto APuenteDto(Puente p) =>
        new(p.Id, p.DiaSemana, p.HoraInicio.ToString("HH:mm:ss"), p.HoraFin.ToString("HH:mm:ss"));

    private static CompaneroCompatibleDto ACompanero(CompaneroCandidato c, ReglasMatching.Compatibilidad m) =>
        new(c.Nombre, c.Carrera, c.Semestre, m.EnComun.Inicio.ToString("HH:mm:ss"), m.EnComun.Fin.ToString("HH:mm:ss"),
            m.MinutosEnComun, m.Porcentaje, m.FranjasCompartidas);
}
