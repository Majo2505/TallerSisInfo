using Breaku.Application.Horarios;
using Breaku.Application.TiempoReal;
using Breaku.Domain.Rules;

namespace Breaku.Application.Matching;

/// <summary>
/// HU12: conectar con quien te sale en «Libres contigo». Saludar solo se puede con alguien que el matching te muestra en
/// ese momento (mismo campus, los dos visibles, con test, y coinciden en tiempo y en el test): se vuelve a calcular aquí,
/// así nadie puede saludar a cualquiera pasando otro id.
/// </summary>
public class ConexionService
{
    private readonly IConexionRepository _repo;
    private readonly IMatchingRepository _matchingRepo;
    private readonly MatchingService _matching;
    private readonly ITiempoReal _tiempoReal;

    public ConexionService(IConexionRepository repo, IMatchingRepository matchingRepo, MatchingService matching, ITiempoReal tiempoReal)
    {
        _repo = repo;
        _matchingRepo = matchingRepo;
        _matching = matching;
        _tiempoReal = tiempoReal;
    }

    public async Task<ConexionesDto> ListarAsync(int usuarioId, CancellationToken ct = default)
    {
        var conexiones = await _repo.ListarDeUsuarioAsync(usuarioId, ct);
        var otros = conexiones.Select(c => Otro(c, usuarioId)).Distinct().ToList();
        var personas = await _repo.ObtenerPersonasAsync(otros, ct);

        var dtos = conexiones
            .Where(c => personas.ContainsKey(Otro(c, usuarioId)))
            .OrderByDescending(c => c.RespondidaEn ?? c.CreadaEn)
            .Select(c => ADto(c, usuarioId, personas[Otro(c, usuarioId)]))
            .ToList();

        return new ConexionesDto(
            Recibidas: dtos.Where(d => !d.EnviadaPorMi && d.Estado == ReglasConexion.Pendiente).ToList(),
            Enviadas: dtos.Where(d => d.EnviadaPorMi && d.Estado == ReglasConexion.Pendiente).ToList(),
            Aceptadas: dtos.Where(d => d.Estado == ReglasConexion.Aceptada).ToList());
    }

    public async Task<ConexionDto> SaludarAsync(int usuarioId, SaludarRequest req, CancellationToken ct = default)
    {
        if (req.UsuarioId is not int otroId || otroId <= 0 || req.PuenteId is not int puenteId || puenteId <= 0)
            throw new ReglaNegocioException("VALIDACION", "Indica a quién saludas (usuarioId) y en cuál de tus puentes (puenteId).");
        if (otroId == usuarioId)
            throw new ReglaNegocioException("VALIDACION", "No puedes saludarte a ti mismo.");

        var existente = await _repo.EntreAsync(usuarioId, otroId, ct);
        if (existente is not null)
            throw new ReglaNegocioException(ReglasConexion.ErrorYaExiste,
                ReglasConexion.MensajeYaExiste(existente.Estado, existente.SolicitanteId == usuarioId));

        if (await _repo.ContarPendientesEnviadasAsync(usuarioId, ct) >= ReglasConexion.MaximoPendientesEnviados)
            throw new ReglaNegocioException(ReglasConexion.ErrorDemasiados, ReglasConexion.MensajeDemasiados);

        var resultado = await _matching.BuscarAsync(usuarioId, puenteId, ct);
        var companero = resultado.Companeros.FirstOrDefault(c => c.UsuarioId == otroId)
                        ?? throw new ReglaNegocioException(ReglasConexion.ErrorNoCoinciden, ReglasConexion.MensajeNoCoinciden);

        var yo = await _matchingRepo.ObtenerUsuarioAsync(usuarioId, ct)
                 ?? throw new ReglaNegocioException("NO_ENCONTRADO", "No encontramos tu cuenta.");

        var inicio = TimeOnly.Parse(companero.HoraInicio);
        var fin = TimeOnly.Parse(companero.HoraFin);
        var nueva = new ConexionNueva(usuarioId, otroId, resultado.Puente.DiaSemana, inicio, fin,
            companero.Compatibilidad, companero.EnComun);
        var aviso = ReglasConexion.MensajeSolicitada(ReglasDescubrir.NombreCorto(yo.Nombre), companero.Compatibilidad,
            resultado.Puente.DiaSemana, inicio, fin);

        var id = await _repo.CrearAsync(nueva, aviso, DateTime.UtcNow, ct);
        await AvisarAsync([otroId], ct);

        var creada = await _repo.ObtenerAsync(id, ct)
                     ?? throw new ReglaNegocioException("NO_ENCONTRADO", "No encontramos el saludo recién enviado.");
        var personas = await _repo.ObtenerPersonasAsync([otroId], ct);
        return ADto(creada, usuarioId, personas[otroId]);
    }

    public async Task<ConexionDto> ResponderAsync(int usuarioId, int conexionId, ResponderConexionRequest req, CancellationToken ct = default)
    {
        if (req.Aceptar is not bool aceptar)
            throw new ReglaNegocioException("VALIDACION", "Indica si aceptas el saludo (aceptar: true o false).");

        var conexion = await _repo.ObtenerAsync(conexionId, ct);
        // A quien no es parte de la conexión se le responde 404, como si no existiera.
        if (conexion is null || (conexion.DestinatarioId != usuarioId && conexion.SolicitanteId != usuarioId))
            throw new ReglaNegocioException("NO_ENCONTRADO", "El saludo no existe.");

        var error = ReglasConexion.ErrorAlResponder(conexion.Estado, conexion.DestinatarioId == usuarioId);
        if (error == "NO_ENCONTRADO")
            throw new ReglaNegocioException("VALIDACION", "Solo quien recibió el saludo puede responderlo.");
        if (error is not null)
            throw new ReglaNegocioException(error, ReglasConexion.MensajeYaRespondida);

        string? avisoAceptada = null;
        if (aceptar)
        {
            var yo = await _matchingRepo.ObtenerUsuarioAsync(usuarioId, ct)
                     ?? throw new ReglaNegocioException("NO_ENCONTRADO", "No encontramos tu cuenta.");
            avisoAceptada = ReglasConexion.MensajeAceptada(ReglasDescubrir.NombreCorto(yo.Nombre), conexion.DiaSemana,
                conexion.HoraInicio, conexion.HoraFin);
        }

        await _repo.ResponderAsync(conexionId, aceptar, avisoAceptada, DateTime.UtcNow, ct);
        await AvisarAsync(aceptar ? [usuarioId, conexion.SolicitanteId] : [usuarioId], ct);

        var respondida = await _repo.ObtenerAsync(conexionId, ct)
                         ?? throw new ReglaNegocioException("NO_ENCONTRADO", "El saludo no existe.");
        var personas = await _repo.ObtenerPersonasAsync([conexion.SolicitanteId], ct);
        return ADto(respondida, usuarioId, personas[conexion.SolicitanteId]);
    }

    private static int Otro(ConexionGuardada c, int usuarioId) => c.SolicitanteId == usuarioId ? c.DestinatarioId : c.SolicitanteId;

    private static ConexionDto ADto(ConexionGuardada c, int usuarioId, PersonaConexion otro)
    {
        var resumen = MatchingService.AResumen(c, usuarioId);
        var aceptada = c.Estado == ReglasConexion.Aceptada;
        return new ConexionDto(
            c.Id, resumen.Estado, resumen.EnviadaPorMi,
            aceptada ? otro.Nombre : ReglasDescubrir.NombreCorto(otro.Nombre),
            otro.Carrera, otro.Semestre, c.Compatibilidad, c.EnComun, c.DiaSemana,
            c.HoraInicio.ToString("HH:mm:ss"), c.HoraFin.ToString("HH:mm:ss"),
            aceptada ? otro.Correo : null,
            c.CreadaEn);
    }

    /// <summary>El tiempo real nunca rompe la operación: si falla, la campanita se pone al día en la siguiente consulta.</summary>
    private async Task AvisarAsync(IReadOnlyCollection<int> usuarios, CancellationToken ct)
    {
        try { await _tiempoReal.AvisosCambiaronAsync(usuarios, ct); }
        catch (Exception) { }
    }
}
