using Breaku.Application.Horarios;
using Breaku.Domain;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

/// <summary>Requiere en el DbContext de Ariana: public DbSet&lt;Horario&gt; Horarios =&gt; Set&lt;Horario&gt;();</summary>
public class HorarioRepository : IHorarioRepository
{
    private readonly AppDbContext _db;   // ajustar al nombre real del DbContext (#139)
    public HorarioRepository(AppDbContext db) => _db = db;

    public Task<List<Horario>> ListarPorUsuarioAsync(int usuarioId, CancellationToken ct = default)
        => _db.Set<Horario>().Where(h => h.UsuarioId == usuarioId).ToListAsync(ct);

    public Task<Horario?> ObtenerAsync(int id, int usuarioId, CancellationToken ct = default)
        => _db.Set<Horario>().FirstOrDefaultAsync(h => h.Id == id && h.UsuarioId == usuarioId, ct);

    public async Task AgregarAsync(Horario horario, CancellationToken ct = default)
        => await _db.Set<Horario>().AddAsync(horario, ct);

    public void Eliminar(Horario horario) => _db.Set<Horario>().Remove(horario);

    public Task GuardarCambiosAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
