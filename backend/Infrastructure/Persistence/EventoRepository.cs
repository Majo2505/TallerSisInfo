using Breaku.Application.Eventos;
using Breaku.Domain;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

public sealed class EventoRepository : IEventosRepository
{
    private readonly AppDbContext _db;
    public EventoRepository(AppDbContext db) => _db = db;

    public async Task UpsertAsync(IReadOnlyList<EventoImportado> eventos, DateTime actualizadoEn, CancellationToken ct = default)
    {
        foreach (var evento in eventos)
        {
            var existente = await _db.Set<EventoUniversitario>().FirstOrDefaultAsync(e => e.FuenteUrl == evento.FuenteUrl, ct);
            if (existente is null)
            {
                _db.Set<EventoUniversitario>().Add(new EventoUniversitario
                {
                    Fuente = "INSTAGRAM",
                    FuenteUrl = evento.FuenteUrl,
                    Titulo = evento.Titulo,
                    Descripcion = evento.Descripcion,
                    Inicio = evento.Inicio,
                    Fin = evento.Fin,
                    Lugar = evento.Lugar,
                    ActualizadoEn = actualizadoEn
                });
            }
            else
            {
                existente.Titulo = evento.Titulo;
                existente.Descripcion = evento.Descripcion;
                existente.Inicio = evento.Inicio;
                existente.Fin = evento.Fin;
                existente.Lugar = evento.Lugar;
                existente.ActualizadoEn = actualizadoEn;
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<EventoUniversitario>> ListarVigentesAsync(DateTime ahoraUtc, DateTime hastaUtc, CancellationToken ct = default)
        => _db.Set<EventoUniversitario>().AsNoTracking()
            .Where(e => e.Inicio < hastaUtc && e.Fin > ahoraUtc)
            .OrderBy(e => e.Inicio)
            .Take(50)
            .ToListAsync(ct);
}
