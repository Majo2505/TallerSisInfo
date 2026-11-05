using Breaku.Application.Actividades;
using Breaku.Application.Horarios;
using Breaku.Domain;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Breaku.Infrastructure.Persistence;

public class ActividadRepository : IActividadRepository
{
    private readonly AppDbContext _db;
    public ActividadRepository(AppDbContext db) => _db = db;

    public Task<LogAuditoriaRag?> ObtenerExtraccionAsync(long consultaId, int usuarioId, CancellationToken ct = default)
        => _db.LogsAuditoriaRag.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == consultaId && l.UsuarioId == usuarioId && l.Tipo == LogAuditoriaRag.TipoExtraccionComunidad, ct);

    public Task<bool> ConsultaYaUsadaAsync(long consultaId, CancellationToken ct = default)
        => _db.Comunidades.AsNoTracking().AnyAsync(c => c.LogId == consultaId, ct);

    public async Task AgregarAsync(ComunidadEfimera comunidad, CancellationToken ct = default)
    {
        _db.Comunidades.Add(comunidad);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry } m
                                           && m.Message.Contains("uq_com_log"))
        {
            // Doble clic en «Confirmar y crear»: los dos pedidos pasan la comprobación previa y la base deja entrar uno solo.
            _db.Entry(comunidad).State = EntityState.Detached;
            throw new ReglaNegocioException("DUPLICADO", "Esa actividad ya se creó.");
        }
    }
}
