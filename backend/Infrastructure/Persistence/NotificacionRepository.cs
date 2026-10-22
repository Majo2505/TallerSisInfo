using Breaku.Application.Notificaciones;
using Breaku.Domain;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Breaku.Infrastructure.Persistence;

public class NotificacionRepository : INotificacionRepository
{
    private readonly AppDbContext _db;
    public NotificacionRepository(AppDbContext db) => _db = db;

    public Task<List<Puente>> ListarPuentesPorAvisarAsync(int diaSemana, DateOnly fecha, TimeOnly desde, TimeOnly hasta, CancellationToken ct = default)
        => (from p in _db.Puentes.AsNoTracking()
            join u in _db.Usuarios on p.UsuarioId equals u.Id
            where p.DiaSemana == diaSemana
                  && p.HoraInicio >= desde && p.HoraInicio <= hasta
                  && u.Activo && u.NotificacionesActivas
                  && !_db.Notificaciones.Any(n => n.UsuarioId == p.UsuarioId
                                                  && n.PuenteFecha == fecha
                                                  && n.PuenteHoraInicio == p.HoraInicio)
            select p).ToListAsync(ct);

    public async Task<bool> AgregarAsync(Notificacion notificacion, CancellationToken ct = default)
    {
        _db.Notificaciones.Add(notificacion);
        try
        {
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
        {
            // Otra ejecución lo insertó entre la consulta y este insert: el UNIQUE evita el aviso repetido.
            _db.Entry(notificacion).State = EntityState.Detached;
            return false;
        }
    }

    public Task<List<Notificacion>> ListarActivasAsync(int usuarioId, int maximo, CancellationToken ct = default)
        => _db.Notificaciones.AsNoTracking()
            .Where(n => n.UsuarioId == usuarioId && n.Activa)
            .OrderByDescending(n => n.CreatedAt)
            .Take(maximo)
            .ToListAsync(ct);

    public Task<List<Notificacion>> ListarActivasDePuenteAsync(int usuarioId, CancellationToken ct = default)
        => _db.Notificaciones.AsNoTracking()
            .Where(n => n.UsuarioId == usuarioId && n.Activa && n.Tipo == Notificacion.TipoPuenteDisponible)
            .ToListAsync(ct);

    public Task DesactivarAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default)
        => _db.Notificaciones.Where(n => ids.Contains(n.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Activa, false), ct);

    public async Task<bool> MarcarLeidaAsync(long id, int usuarioId, CancellationToken ct = default)
        // MySqlConnector cuenta filas encontradas (no solo modificadas): marcar dos veces sigue devolviendo 1.
        => await _db.Notificaciones.Where(n => n.Id == id && n.UsuarioId == usuarioId && n.Activa)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Leida, true), ct) > 0;

    public async Task<bool?> ObtenerPreferenciaAsync(int usuarioId, CancellationToken ct = default)
        => await _db.Usuarios.AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => (bool?)u.NotificacionesActivas)
            .FirstOrDefaultAsync(ct);

    public Task GuardarPreferenciaAsync(int usuarioId, bool activas, CancellationToken ct = default)
        => _db.Usuarios.Where(u => u.Id == usuarioId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.NotificacionesActivas, activas)
                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow), ct);
}
