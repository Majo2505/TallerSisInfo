using Breaku.Application.Sanciones;
using Breaku.Domain;
using Breaku.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

/// <summary>HU28: lo que guarda y lee la gestión de sanciones. Las reglas viven en <see cref="ReglasSancion"/>.</summary>
public sealed class SancionRepository : ISancionRepository
{
    private readonly AppDbContext _db;
    public SancionRepository(AppDbContext db) => _db = db;

    public async Task<List<DatosUsuario>> ListarEstudiantesAsync(string? buscar, int limite, CancellationToken ct = default)
    {
        var consulta = _db.Usuarios.AsNoTracking().Where(u => u.Rol == "ESTUDIANTE");
        if (!string.IsNullOrWhiteSpace(buscar))
            consulta = consulta.Where(u => u.Nombre.Contains(buscar) || u.Email.Contains(buscar));

        var filas = await consulta.OrderBy(u => u.Nombre).ThenBy(u => u.Id).Take(limite).ToListAsync(ct);
        return filas.Select(ADatos).ToList();
    }

    public async Task<List<RegistroDeUsuario>> ListarRegistrosAsync(IReadOnlyCollection<int> usuarioIds, CancellationToken ct = default)
    {
        var filas = await _db.Sanciones.AsNoTracking().Where(s => usuarioIds.Contains(s.UsuarioId)).ToListAsync(ct);
        return filas.Select(s => new RegistroDeUsuario(s.UsuarioId, ARegistro(s))).ToList();
    }

    public async Task<DatosUsuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default)
    {
        var usuario = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuarioId, ct);
        return usuario is null ? null : ADatos(usuario);
    }

    public async Task<List<SancionDetalle>> ListarSancionesAsync(int usuarioId, CancellationToken ct = default)
    {
        var filas = await Detalle().Where(f => f.Sancion.UsuarioId == usuarioId).ToListAsync(ct);
        return filas.Select(f => new SancionDetalle(ARegistro(f.Sancion), f.Sancion.ReporteId, f.Creador, f.Levanta)).ToList();
    }

    public async Task<SancionDetalle?> ObtenerSancionAsync(int usuarioId, int sancionId, CancellationToken ct = default)
    {
        var fila = await Detalle().Where(f => f.Sancion.UsuarioId == usuarioId && f.Sancion.Id == sancionId).FirstOrDefaultAsync(ct);
        return fila is null ? null : new SancionDetalle(ARegistro(fila.Sancion), fila.Sancion.ReporteId, fila.Creador, fila.Levanta);
    }

    public async Task<bool> ReporteExisteAsync(int reporteId, CancellationToken ct = default)
        => (await _db.Database.SqlQuery<int>($"SELECT id AS Value FROM reporte WHERE id = {reporteId}").ToListAsync(ct)).Count > 0;

    public async Task BloquearUsuarioAsync(int usuarioId, CancellationToken ct = default)
        => await _db.Database.SqlQuery<int>($"SELECT id AS Value FROM usuario WHERE id = {usuarioId} FOR UPDATE").ToListAsync(ct);

    public async Task<int> AgregarAsync(int usuarioId, ReglasSancion.SancionValida sancion, int? reporteId, int adminId, DateTime ahoraUtc,
        CancellationToken ct = default)
    {
        var fila = new Sancion
        {
            UsuarioId = usuarioId,
            Tipo = sancion.Tipo,
            Motivo = sancion.Motivo,
            ReporteId = reporteId,
            CreadoPor = adminId,
            CreadoEn = ahoraUtc,
            Hasta = sancion.Hasta
        };
        _db.Sanciones.Add(fila);
        await _db.SaveChangesAsync(ct);
        return fila.Id;
    }

    public async Task<bool> LevantarAsync(int sancionId, int adminId, DateTime ahoraUtc, CancellationToken ct = default)
        => await _db.Sanciones.Where(s => s.Id == sancionId && s.LevantadaEn == null)
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.LevantadaEn, ahoraUtc).SetProperty(s => s.LevantadaPor, adminId), ct) > 0;

    public async Task AgregarAvisoAdvertenciaAsync(int usuarioId, int sancionId, string mensaje, DateTime ahoraUtc, CancellationToken ct = default)
    {
        _db.Notificaciones.Add(new Notificacion
        {
            UsuarioId = usuarioId,
            Tipo = Notificacion.TipoSancionAdvertencia,
            SancionId = sancionId,
            Mensaje = mensaje,
            CreatedAt = ahoraUtc
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<EstadoSesion?> ObtenerEstadoSesionAsync(int usuarioId, DateTime ahoraUtc, CancellationToken ct = default)
    {
        var activo = await _db.Usuarios.AsNoTracking().Where(u => u.Id == usuarioId).Select(u => (bool?)u.Activo).FirstOrDefaultAsync(ct);
        if (activo is null) return null;

        var vigentes = await _db.Sanciones.AsNoTracking()
            .Where(s => s.UsuarioId == usuarioId && s.LevantadaEn == null
                        && (s.Tipo == Sancion.TipoBaja || (s.Tipo == Sancion.TipoSuspension && s.Hasta > ahoraUtc)))
            .ToListAsync(ct);
        return new EstadoSesion(true, ReglasSancion.BloqueoVigente(activo.Value, vigentes.Select(ARegistro), ahoraUtc));
    }

    private IQueryable<FilaDetalle> Detalle()
        => from s in _db.Sanciones.AsNoTracking()
           join a in _db.Usuarios.AsNoTracking() on s.CreadoPor equals a.Id
           join l in _db.Usuarios.AsNoTracking() on s.LevantadaPor equals l.Id into levantadores
           from l in levantadores.DefaultIfEmpty()
           select new FilaDetalle { Sancion = s, Creador = a.Nombre, Levanta = l == null ? null : l.Nombre };

    private sealed class FilaDetalle
    {
        public Sancion Sancion { get; set; } = null!;
        public string Creador { get; set; } = "";
        public string? Levanta { get; set; }
    }

    private static DatosUsuario ADatos(Usuario u) => new(u.Id, u.Nombre, u.Email, u.Carrera, u.Semestre, u.Rol, u.Activo, u.CreatedAt);

    internal static ReglasSancion.Registro ARegistro(Sancion s) => new(s.Id, s.Tipo, s.Motivo, s.CreadoEn, s.Hasta, s.LevantadaEn);
}
