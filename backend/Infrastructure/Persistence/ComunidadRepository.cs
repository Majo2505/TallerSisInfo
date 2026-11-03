using Breaku.Application.Comunidades;
using Breaku.Application.Horarios;
using Breaku.Domain;
using Breaku.Domain.Rules;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Breaku.Infrastructure.Persistence;

public sealed class ComunidadRepository : IComunidadRepository
{
    private readonly AppDbContext _db;
    public ComunidadRepository(AppDbContext db) => _db = db;

    public Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default)
        => _db.Usuarios.AsNoTracking().Include(u => u.ZonaActual).FirstOrDefaultAsync(u => u.Id == usuarioId && u.Activo, ct);

    public Task<LogAuditoriaRag?> ObtenerExtraccionAsync(long consultaId, CancellationToken ct = default)
        => _db.LogsAuditoriaRag.AsNoTracking().FirstOrDefaultAsync(l => l.Id == consultaId, ct);

    public Task<bool> ExisteComunidadDeConsultaAsync(long consultaId, CancellationToken ct = default)
        => _db.Set<ComunidadEfimera>().AnyAsync(c => c.LogId == consultaId, ct);

    public Task<bool> EstaEnOtraComunidadAsync(int usuarioId, DateTime inicioUtc, DateTime finUtc, CancellationToken ct = default)
        => OcupadosEn(inicioUtc, finUtc).AnyAsync(id => id == usuarioId, ct);

    public async Task<List<CandidatoComunidad>> ListarCandidatosAsync(int usuarioId, int zonaId, ReglasMatching.Franja actividad,
        DateTime inicioUtc, DateTime finUtc, CancellationToken ct = default)
    {
        var (diaSemana, inicio, fin) = (actividad.DiaSemana, actividad.Inicio, actividad.Fin);
        // Mismo filtro que el matching de HU12 (sede, consentimiento, puente que se solapa; usa ix_usuario_matching e
        // ix_puente_matching) y, además, que no estén ya en otra comunidad a esa hora. El mínimo en común lo aplica ReglasComunidad.
        var ocupados = OcupadosEn(inicioUtc, finUtc);
        var companeros = await _db.Usuarios.AsNoTracking()
            .Where(u => u.Id != usuarioId && u.Activo && u.Rol == "ESTUDIANTE" && u.VisibleMatching && u.ZonaActualId == zonaId)
            .Where(u => _db.Puentes.Any(p => p.UsuarioId == u.Id && p.DiaSemana == diaSemana && p.HoraInicio < fin && p.HoraFin > inicio))
            .Where(u => !ocupados.Contains(u.Id))
            .Select(u => new { u.Id, u.Nombre, u.Carrera, u.NotificacionesActivas })
            .ToListAsync(ct);
        if (companeros.Count == 0) return [];

        var ids = companeros.Select(c => c.Id).ToList();
        var puentes = await _db.Puentes.AsNoTracking()
            .Where(p => ids.Contains(p.UsuarioId) && p.DiaSemana == diaSemana)
            .ToListAsync(ct);
        var porUsuario = puentes.ToLookup(p => p.UsuarioId);

        return companeros
            .Select(c => new CandidatoComunidad(c.Id, c.Nombre, c.Carrera, c.NotificacionesActivas, porUsuario[c.Id].ToList()))
            .ToList();
    }

    public async Task CrearAsync(ComunidadEfimera comunidad, IReadOnlyList<Participacion> participaciones, IReadOnlyList<Notificacion> avisos,
        CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            _db.Set<ComunidadEfimera>().Add(comunidad);
            await _db.SaveChangesAsync(ct);

            foreach (var p in participaciones) p.ComunidadId = comunidad.Id;
            foreach (var a in avisos) a.ComunidadId = comunidad.Id;
            _db.Set<Participacion>().AddRange(participaciones);
            _db.Notificaciones.AddRange(avisos);
            await _db.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry } mysql
                                           && mysql.Message.Contains("uq_com_log"))
        {
            // Dos pedidos a la vez con la misma extracción: la base deja pasar solo uno.
            throw new ReglaNegocioException("YA_CREADA", "Ya creaste una comunidad con esta actividad.");
        }
    }

    /// <summary>
    /// Ids de quienes ya están en una comunidad activa que se cruza con ese horario: la crearon o confirmaron que van.
    /// Una invitación pendiente no cuenta, porque todavía no aceptaron.
    /// </summary>
    private IQueryable<int> OcupadosEn(DateTime inicioUtc, DateTime finUtc)
        => from p in _db.Set<Participacion>()
           join c in _db.Set<ComunidadEfimera>() on p.ComunidadId equals c.Id
           where p.Estado == Participacion.EstadoConfirmado && c.Estado == ComunidadEfimera.EstadoActiva
                 && c.Inicio < finUtc && c.Fin > inicioUtc
           select p.UsuarioId;
}
