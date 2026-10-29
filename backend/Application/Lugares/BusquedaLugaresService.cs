using Breaku.Application.Horarios;
using Breaku.Application.Puentes;
using Breaku.Application.Recomendaciones;
using Breaku.Application.Ubicacion;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Lugares;

/// <summary>
/// Caso de uso de HU6: lugares viables para un puente según el tiempo disponible, la cercanía al campus del estudiante
/// y la zona verificada. También es la recuperación del asistente de IA (HU7): la recomendación solo ve estos lugares.
/// </summary>
public class BusquedaLugaresService : IRecuperadorLugares
{
    public const string EstadoOk = "OK";
    public const string EstadoSinResultados = "SIN_RESULTADOS";
    public const string EstadoSinZona = "SIN_ZONA";

    private readonly ILugarRepository _lugares;
    private readonly IPuenteRepository _puentes;

    public BusquedaLugaresService(ILugarRepository lugares, IPuenteRepository puentes)
    {
        _lugares = lugares;
        _puentes = puentes;
    }

    public async Task<ResultadoBusquedaLugaresDto> BuscarParaPuenteAsync(int usuarioId, int puenteId, CancellationToken ct = default)
    {
        var puente = (await _puentes.ListarPorUsuarioAsync(usuarioId, ct)).FirstOrDefault(p => p.Id == puenteId)
                     ?? throw new ReglaNegocioException("NO_ENCONTRADO", "El puente no existe.");
        var disponible = MinutosDelPuente(puente);

        var (zona, candidatos) = await BuscarAsync(usuarioId, puente, ReglasBusquedaLugares.MaximoLugaresListado, ct);

        if (zona is null)
            return new ResultadoBusquedaLugaresDto(EstadoSinZona, ReglasBusquedaLugares.MensajeSinZona, null, null, disponible, []);

        var zonaDto = new ZonaDto(zona.Id, zona.Nombre);
        if (candidatos.Count == 0)
            return new ResultadoBusquedaLugaresDto(EstadoSinResultados, ReglasBusquedaLugares.MensajeSinResultados, zonaDto, zona.RadioMaxM, disponible, []);

        return new ResultadoBusquedaLugaresDto(EstadoOk, null, zonaDto, zona.RadioMaxM, disponible, candidatos.Select(ADto).ToList());
    }

    public async Task<ContextoRecuperado> RecuperarAsync(int usuarioId, Puente puente, CancellationToken ct = default)
    {
        var (zona, candidatos) = await BuscarAsync(usuarioId, puente, ReglasBusquedaLugares.MaximoCandidatosAsistente, ct);
        return new ContextoRecuperado(zona?.Id, zona?.RadioMaxM, candidatos);
    }

    private async Task<(Zona? Zona, List<LugarCandidato> Candidatos)> BuscarAsync(
        int usuarioId, Puente puente, int maximo, CancellationToken ct)
    {
        var zona = await _lugares.ObtenerZonaDelUsuarioAsync(usuarioId, ct);
        if (zona is null) return (null, []);

        var disponible = MinutosDelPuente(puente);
        var activos = (await _lugares.ListarActivosAsync(ct))
            .Where(l => l.Estado == ReglasBusquedaLugares.EstadoActivo);

        var candidatos = activos
            .Select(l =>
            {
                var distancia = (int)Math.Round(ReglasBusquedaLugares.DistanciaMetros(zona.Latitud, zona.Longitud, l.Latitud, l.Longitud));
                var traslado = ReglasBusquedaLugares.TiempoTrasladoIdaVueltaMin(distancia);
                return new LugarCandidato(l.Id, l.Nombre, l.Categoria.Nombre, l.Descripcion, distancia, traslado, l.DuracionEstimadaMin,
                    l.Validado, l.TipoLugar, ListasLugar.SepararPorComas(l.Modalidad), ListasLugar.SepararPorComas(l.NivelEnergia),
                    ListasLugar.LeerJson(l.ActividadesSugeridas));
            })
            .Where(c => ReglasBusquedaLugares.EstaDentroDelRadio(c.DistanciaM, zona.RadioMaxM))
            .Where(c => ReglasBusquedaLugares.CabeEnElPuente(c.TiempoTrasladoMin, c.DuracionEstimadaMin, disponible))
            .OrderBy(c => c.DistanciaM)
            .ThenBy(c => c.Nombre)
            .Take(maximo)
            .ToList();

        return (zona, candidatos);
    }

    private static int MinutosDelPuente(Puente puente) => (int)(puente.HoraFin - puente.HoraInicio).TotalMinutes;

    private static LugarViableDto ADto(LugarCandidato c) =>
        new(c.LugarId, c.Nombre, c.Categoria, c.Descripcion, c.DistanciaM, c.TiempoTrasladoMin, c.DuracionEstimadaMin,
            c.Validado, c.TipoLugar, c.Modalidad ?? [], c.NivelEnergia ?? [], c.ActividadesSugeridas ?? []);
}
