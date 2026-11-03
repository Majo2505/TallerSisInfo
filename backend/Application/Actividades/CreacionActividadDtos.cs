using Breaku.Domain;

namespace Breaku.Application.Actividades;

/// <summary>Port: lo implementa Infrastructure.</summary>
public interface IActividadRepository
{
    /// <summary>La extracción de actividad (HU11) con ese id, solo si es del usuario. <c>null</c> si no existe o es de otro.</summary>
    Task<LogAuditoriaRag?> ObtenerExtraccionAsync(long consultaId, int usuarioId, CancellationToken ct = default);

    /// <summary>¿Ya hay una actividad creada a partir de esa extracción?</summary>
    Task<bool> ConsultaYaUsadaAsync(long consultaId, CancellationToken ct = default);

    Task AgregarAsync(ComunidadEfimera comunidad, CancellationToken ct = default);
}

/// <summary>Actividad que el estudiante confirmó. Los datos son los del resumen de <c>POST /api/actividades/extraer</c>.</summary>
/// <param name="Texto" example="quiero jugar cartas por una hora">Lo que escribió el estudiante (hasta 200 caracteres).</param>
/// <param name="Actividad" example="jugar cartas">Qué se va a hacer.</param>
/// <param name="HoraInicio" example="16:00">Hora de inicio (<c>HH:mm</c>), de hoy y en hora de Bolivia.</param>
/// <param name="DuracionMin" example="60">Duración en minutos.</param>
/// <param name="MaxParticipantes" example="4">Personas en total, contando a quien la crea.</param>
/// <param name="ConsultaId" example="58"><c>consultaId</c> de la extracción completa de la que salió la actividad.</param>
public record CrearActividadRequest(string Texto, string Actividad, string HoraInicio, int DuracionMin, int MaxParticipantes, long ConsultaId);

/// <summary>Actividad creada.</summary>
/// <param name="Id" example="12">Identificador de la comunidad.</param>
/// <param name="Actividad" example="jugar cartas">Qué se va a hacer.</param>
/// <param name="HoraInicio" example="16:00">Hora de inicio, hora de Bolivia.</param>
/// <param name="HoraFin" example="17:00">Hora de fin, hora de Bolivia.</param>
/// <param name="DuracionMin" example="60">Duración en minutos.</param>
/// <param name="MaxParticipantes" example="4">Personas en total, contando a quien la crea.</param>
/// <param name="Estado" example="ACTIVA">Estado de la comunidad.</param>
/// <param name="Invitacion">HU13: a quiénes se invitó automáticamente (compañeros libres a esa hora) o por qué no se invitó a nadie.</param>
public record ActividadCreadaDto(int Id, string Actividad, string HoraInicio, string HoraFin, int DuracionMin, int MaxParticipantes, string Estado,
    Breaku.Application.Comunidades.InvitacionDto? Invitacion = null);
