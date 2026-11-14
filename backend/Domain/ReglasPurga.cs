namespace Breaku.Domain.Rules;

/// <summary>
/// HU28/HU29: la purga de los datos de un usuario (baja de HU28, eliminación de cuenta de HU29). Reglas puras: el orden
/// de los pasos y qué se hace con cada comunidad que creó el usuario. Lo que ejecuta cada paso lo hace Infrastructure.
/// </summary>
public static class ReglasPurga
{
    /// <summary>
    /// Los pasos, en el orden de las claves foráneas (ninguna lleva ON DELETE CASCADE salvo <c>evento_imagen</c>):
    /// primero lo que apunta a otras filas y al final lo apuntado.
    /// </summary>
    public enum Paso
    {
        /// <summary>Comunidades que creó: las que siguen abiertas cambian de creador (con <c>log_id</c> NULL y texto vacío) y el resto se borra.</summary>
        ComunidadesCreadas = 1,
        /// <summary>Participaciones del usuario en comunidades de otros.</summary>
        Participaciones,
        /// <summary>Todas sus notificaciones (apuntan a comunidades, eventos, reportes y sanciones).</summary>
        Notificaciones,
        /// <summary>HU12: sus saludos (enviados y recibidos), los avisos de OTROS que apuntan a ellos y su test de afinidad.</summary>
        Conexiones,
        /// <summary>Sus eventos: antes, las notificaciones y los reportes de OTROS usuarios que apuntan a ellos; las fotos se borran en cascada.</summary>
        Eventos,
        /// <summary>Los reportes que envió (las notificaciones que apuntan a ellos ya se borraron).</summary>
        Reportes,
        Recomendaciones,
        LugaresRecuperados,
        /// <summary>Después de poner en NULL el <c>log_id</c> de las comunidades que sobreviven.</summary>
        LogsAuditoria,
        Puentes,
        Horarios,
        HistorialCambio,
        /// <summary>Solo en la eliminación de cuenta: las notificaciones que apuntaban a ellas ya se borraron.</summary>
        Sanciones,
        /// <summary>La baja anonimiza la fila de <c>usuario</c>; la eliminación de cuenta la borra.</summary>
        Usuario
    }

    /// <summary>Los pasos de la purga, ya en orden. La baja conserva las filas de <c>sancion</c>.</summary>
    public static IReadOnlyList<Paso> Pasos(bool eliminarCuenta) =>
        Enum.GetValues<Paso>().Where(p => eliminarCuenta || p != Paso.Sanciones).OrderBy(p => (int)p).ToList();

    public sealed record ParticipanteComunidad(int UsuarioId, string Estado, DateTime? RespondidoEn);

    public sealed record ComunidadCreada(int Id, string Estado, IReadOnlyList<ParticipanteComunidad> Participantes);

    public enum Destino { Eliminar, CambiarCreador }

    public sealed record DecisionComunidad(int ComunidadId, Destino Destino, int? NuevoCreadorId, int ConfirmadosRestantes);

    /// <summary>
    /// Qué pasa con una comunidad que creó el usuario. Solo cuentan como «otros participantes» los CONFIRMADO:
    /// · ARCHIVADA → se elimina (no hay nada que mantener abierto).
    /// · ACTIVA con otros CONFIRMADO → sigue abierta y el creador pasa al de menor <c>respondido_en</c> (empate: menor id).
    /// · ACTIVA sin otros CONFIRMADO → se elimina, con sus participaciones PENDIENTE y RECHAZADO.
    /// </summary>
    public static DecisionComunidad DecidirComunidad(int usuarioId, ComunidadCreada comunidad)
    {
        var otros = comunidad.Participantes
            .Where(p => p.UsuarioId != usuarioId && p.Estado == Participacion.EstadoConfirmado)
            .ToList();

        if (comunidad.Estado != ComunidadEfimera.EstadoActiva || otros.Count == 0)
            return new DecisionComunidad(comunidad.Id, Destino.Eliminar, null, 0);

        var nuevo = otros.OrderBy(p => p.RespondidoEn ?? DateTime.MaxValue).ThenBy(p => p.UsuarioId).First();
        return new DecisionComunidad(comunidad.Id, Destino.CambiarCreador, nuevo.UsuarioId, otros.Count);
    }
}
