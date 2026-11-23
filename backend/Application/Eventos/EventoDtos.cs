using Breaku.Domain;

namespace Breaku.Application.Eventos;

public sealed record EventoFormularioRequest(
    string? Titulo,
    string? Descripcion,
    DateTimeOffset? Inicio,
    DateTimeOffset? Fin,
    int? LugarId,
    string? LugarTexto,
    string? Asistencia,
    int? VisitaMin,
    int? VisitaMax,
    // FORMULARIO (por defecto) o FLYER cuando el borrador salió de leer un afiche. Solo se usa al crear.
    string? Origen = null,
    // Solo al editar: borra la foto actual del evento. No se puede combinar con una foto nueva. Al crear se ignora.
    bool QuitarFoto = false);

public sealed record EventoDto(
    int Id,
    string Titulo,
    string? Descripcion,
    DateTimeOffset Inicio,
    DateTimeOffset Fin,
    int? LugarId,
    string Lugar,
    string Estado,
    string Origen,
    string Asistencia,
    short? VisitaMin,
    short? VisitaMax,
    string? Motivo,
    string? FotoUrl,
    DateTimeOffset CreatedAt);

public sealed record EventoLugarDto(int Id, string Nombre);

public sealed record EventoListado(Evento Evento, string NombreLugar, DateTime? FotoActualizadaEn);

public sealed record EventoParaPuente(Evento Evento, Lugar Lugar);

public sealed record EventoEnPuenteDto(
    int Id,
    string Titulo,
    string Inicio,
    string Fin,
    string Lugar,
    string Asistencia,
    int? MinutosPuedeQuedarse,
    string Explicacion);

public sealed record FotoEvento(
    byte[] Contenido,
    string TipoMime,
    DateTime ActualizadaEn,
    int CreadorId,
    string Estado,
    bool CreadorSuspendido);

/// <summary>
/// Aprobar (<c>PUBLICADO</c>) o rechazar (<c>RECHAZADO</c>, con motivo) un evento pendiente. Con <c>Cambios</c>, el admin
/// lo edita y lo aprueba en un solo paso (solo al aprobar). La foto no se cambia aquí.
/// </summary>
public sealed record ResolverEventoRequest(string? Estado, string? Motivo, EventoFormularioRequest? Cambios = null);

/// <summary>Un evento RECHAZADO tal como lo lee la base, con quién y cuándo lo moderó (si quedó en historial_cambio).</summary>
public sealed record EventoRechazado(EventoListado Listado, bool FueBajado, string? Admin, DateTime? RechazadoEnUtc);

/// <summary>
/// HU26: un evento del historial de rechazados. <c>FueBajado</c> = estaba PUBLICADO y el admin lo bajó; si no, se rechazó
/// en la revisión. <c>Admin</c> y <c>RechazadoEn</c> salen de historial_cambio; los rechazados antes de HU26 no los tienen,
/// y entonces <c>RechazadoEn</c> es la última actualización del evento.
/// </summary>
public sealed record EventoRechazadoDto(EventoDto Evento, bool FueBajado, string? Admin, DateTimeOffset RechazadoEn);

/// <summary>Bajar un evento ya publicado: queda RECHAZADO, oculto y sin borrarse. El motivo es obligatorio.</summary>
public sealed record BajarEventoRequest(string? Motivo);

/// <summary>Datos ya validados con los que el admin corrige un evento al aprobarlo.</summary>
public sealed record CambiosEvento(
    string Titulo,
    string? Descripcion,
    int? LugarId,
    string? LugarTexto,
    DateTime InicioUtc,
    DateTime FinUtc,
    string Asistencia,
    short? VisitaMin,
    short? VisitaMax);

/// <summary>
/// Una acción de moderación: el evento tiene que estar en <c>EstadoEsperado</c> y pasa a <c>EstadoNuevo</c>. El cambio, el
/// registro en historial_cambio (con el admin) y el aviso al autor (<c>TipoAviso</c>) se guardan en una sola transacción.
/// </summary>
public sealed record ModeracionEvento(
    int EventoId,
    int AdminId,
    string EstadoEsperado,
    string EstadoNuevo,
    string? Motivo,
    string TipoAviso,
    CambiosEvento? Cambios,
    DateTime AhoraUtc);

public enum ResultadoModeracionEvento
{
    Actualizado,
    NoEncontrado,
    YaRevisado,
    Finalizado
}

public sealed record EventoPublicoDto(
    int Id,
    string Titulo,
    string? Descripcion,
    DateTimeOffset Inicio,
    DateTimeOffset Fin,
    int? LugarId,
    string Lugar,
    bool FueraDelCampus,
    string Asistencia,
    short? VisitaMin,
    short? VisitaMax,
    string? FotoUrl);
