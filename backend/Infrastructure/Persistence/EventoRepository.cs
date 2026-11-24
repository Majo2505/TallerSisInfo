using Breaku.Application.Eventos;
using Breaku.Application.Horarios;
using Breaku.Domain;
using Breaku.Domain.Rules;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

public sealed class EventoRepository : IEventoRepository
{
    private readonly AppDbContext _db;

    public EventoRepository(AppDbContext db) => _db = db;

    public Task<List<Lugar>> ListarLugaresDisponiblesAsync(CancellationToken ct = default)
        => _db.Lugares.AsNoTracking()
            .Where(l => l.Estado == ReglasBusquedaLugares.EstadoActivo && l.Validado)
            .ToListAsync(ct);

    public Task<Lugar?> ObtenerLugarAsync(int lugarId, CancellationToken ct = default)
        => _db.Lugares.AsNoTracking().FirstOrDefaultAsync(l => l.Id == lugarId, ct);

    public async Task<List<EventoParaPuente>> ListarPublicadosHoyAsync(
        DateTime desdeUtc,
        DateTime hastaUtc,
        DateTime ahoraUtc,
        CancellationToken ct = default)
    {
        var suspendidos = FiltrosSancion.IdsSuspendidos(_db, ahoraUtc);
        var eventos = await _db.Eventos.AsNoTracking()
            .Include(e => e.Lugar)
            .Where(e => e.Estado == Evento.EstadoPublicado
                && e.LugarId != null
                && e.Inicio >= desdeUtc
                && e.Inicio < hastaUtc
                && e.Fin > ahoraUtc
                && !suspendidos.Contains(e.CreadorId))
            .OrderBy(e => e.Inicio)
            .ToListAsync(ct);

        return eventos
            .Where(e => e.Lugar is not null)
            .Select(e => new EventoParaPuente(e, e.Lugar!))
            .ToList();
    }

    public async Task<bool> CrearAsync(
        Evento evento,
        byte[]? imagen,
        string? tipoMime,
        bool aplicarLimiteDiario,
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        if (aplicarLimiteDiario)
        {
            var usuarioBloqueado = await _db.Database
                .SqlQuery<int>($"SELECT id AS Value FROM usuario WHERE id = {evento.CreadorId} FOR UPDATE")
                .ToListAsync(ct);
            if (usuarioBloqueado.Count == 0)
                throw new ReglaNegocioException("NO_ENCONTRADO", "No se encontró el estudiante.");

            var creadosHoy = await _db.Eventos.CountAsync(e =>
                e.CreadorId == evento.CreadorId && e.CreatedAt >= desdeUtc && e.CreatedAt < hastaUtc, ct);
            if (creadosHoy >= ReglasEvento.MaximoEventosPorDia) return false;
        }

        _db.Eventos.Add(evento);
        await _db.SaveChangesAsync(ct);
        if (imagen is not null && tipoMime is not null)
            await GuardarImagenAsync(evento.Id, imagen, tipoMime, evento.CreatedAt, ct);

        await tx.CommitAsync(ct);
        return true;
    }

    public Task<List<ReglasEvento.CandidatoDuplicado>> BuscarCandidatosDuplicadoAsync(
        DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default)
        => _db.Eventos.AsNoTracking()
            .Include(e => e.Lugar)
            .Where(e => (e.Estado == Evento.EstadoPublicado || e.Estado == Evento.EstadoPendiente)
                && e.Fin > desdeUtc
                && e.Inicio < hastaUtc)
            .Select(e => new ReglasEvento.CandidatoDuplicado(
                e.Id, e.CreadorId, e.Titulo, e.Inicio, e.Fin, e.LugarId, e.LugarTexto,
                e.Lugar != null ? e.Lugar.Nombre : null, e.Estado))
            .ToListAsync(ct);

    public async Task<List<EventoListado>> ListarMiosAsync(int usuarioId, CancellationToken ct = default)
        => await ListarAsync(_db.Eventos.AsNoTracking().Where(e => e.CreadorId == usuarioId), ct);

    public async Task<Evento?> ObtenerMioAsync(int eventoId, int usuarioId, CancellationToken ct = default)
        => await _db.Eventos.FirstOrDefaultAsync(e => e.Id == eventoId && e.CreadorId == usuarioId, ct);

    public async Task GuardarCambiosAsync(
        Evento evento, byte[]? imagen, string? tipoMime, bool quitarImagen = false, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        await _db.SaveChangesAsync(ct);
        if (imagen is not null && tipoMime is not null)
            await GuardarImagenAsync(evento.Id, imagen, tipoMime, evento.UpdatedAt, ct);
        else if (quitarImagen)
            await _db.Database.ExecuteSqlAsync($"DELETE FROM evento_imagen WHERE evento_id = {evento.Id}", ct);
        await tx.CommitAsync(ct);
    }

    public async Task<FotoEvento?> ObtenerImagenAsync(int eventoId, CancellationToken ct = default)
    {
        var fila = (await _db.Database.SqlQuery<FilaFoto>($"""
                SELECT i.contenido AS Contenido, i.tipo_mime AS TipoMime, i.actualizada_en AS ActualizadaEn,
                       e.creador_id AS CreadorId, e.estado AS Estado
                FROM evento_imagen i
                JOIN evento e ON e.id = i.evento_id
                WHERE i.evento_id = {eventoId}
                """).ToListAsync(ct)).FirstOrDefault();

        if (fila is null) return null;

        var suspendido = await FiltrosSancion.IdsSuspendidos(_db, DateTime.UtcNow)
            .AnyAsync(id => id == fila.CreadorId, ct);

        return new FotoEvento(fila.Contenido, fila.TipoMime,
            DateTime.SpecifyKind(fila.ActualizadaEn, DateTimeKind.Utc), fila.CreadorId, fila.Estado, suspendido);
    }

    public Task<List<EventoListado>> ListarPublicadosAsync(DateTime ahoraUtc, DateTime hastaUtc, CancellationToken ct = default)
    {
        var suspendidos = FiltrosSancion.IdsSuspendidos(_db, ahoraUtc);
        return ListarAsync(_db.Eventos.AsNoTracking()
            .Where(e => e.Estado == Evento.EstadoPublicado
                && e.Fin > ahoraUtc
                && e.Inicio < hastaUtc
                && !suspendidos.Contains(e.CreadorId)), ct);
    }

    public async Task<EventoListado?> ObtenerPublicadoAsync(int eventoId, DateTime ahoraUtc, CancellationToken ct = default)
    {
        var suspendidos = FiltrosSancion.IdsSuspendidos(_db, ahoraUtc);
        var lista = await ListarAsync(_db.Eventos.AsNoTracking()
            .Where(e => e.Id == eventoId
                && e.Estado == Evento.EstadoPublicado
                && !suspendidos.Contains(e.CreadorId)), ct);
        return lista.FirstOrDefault();
    }

    public Task<List<EventoListado>> ListarPendientesAsync(DateTime ahoraUtc, CancellationToken ct = default)
        => ListarAsync(_db.Eventos.AsNoTracking()
            .Where(e => e.Estado == Evento.EstadoPendiente && e.Fin > ahoraUtc), ct);

    public async Task<List<EventoRechazado>> ListarRechazadosAsync(int maximo, CancellationToken ct = default)
    {
        var ids = await _db.Eventos.AsNoTracking()
            .Where(e => e.Estado == Evento.EstadoRechazado)
            .OrderByDescending(e => e.UpdatedAt)
            .Take(maximo)
            .Select(e => e.Id)
            .ToListAsync(ct);
        if (ids.Count == 0) return [];

        var listados = await ListarAsync(_db.Eventos.AsNoTracking().Where(e => ids.Contains(e.Id)), ct);

        // La última moderación que dejó cada evento en RECHAZADO (HU26). Los rechazados antes de HU26 no tienen fila.
        var lista = string.Join(",", ids);
        var moderaciones = await _db.Database.SqlQueryRaw<FilaModeracion>($"""
                SELECT h.entidad_id AS EventoId,
                       JSON_UNQUOTE(JSON_EXTRACT(h.valores_anteriores, '$.estado')) AS EstadoAnterior,
                       u.nombre AS Admin,
                       h.created_at AS Fecha
                FROM historial_cambio h
                LEFT JOIN usuario u ON u.id = h.usuario_id
                WHERE h.entidad = 'EVENTO' AND h.entidad_id IN ({lista})
                  AND JSON_UNQUOTE(JSON_EXTRACT(h.valores_nuevos, '$.estado')) = 'RECHAZADO'
                ORDER BY h.created_at DESC, h.id DESC
                """).ToListAsync(ct);
        var ultima = moderaciones.GroupBy(m => m.EventoId).ToDictionary(g => g.Key, g => g.First());

        return listados.Select(l =>
        {
            var m = ultima.GetValueOrDefault(l.Evento.Id);
            return new EventoRechazado(
                l,
                m?.EstadoAnterior == Evento.EstadoPublicado,
                m?.Admin,
                m is null ? null : DateTime.SpecifyKind(m.Fecha, DateTimeKind.Utc));
        }).ToList();
    }

    private sealed class FilaModeracion
    {
        public int EventoId { get; set; }
        public string? EstadoAnterior { get; set; }
        public string? Admin { get; set; }
        public DateTime Fecha { get; set; }
    }

    public async Task<ResultadoModeracionEvento> ModerarAsync(ModeracionEvento m, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var bloqueado = await _db.Database
            .SqlQuery<int>($"SELECT id AS Value FROM evento WHERE id = {m.EventoId} FOR UPDATE")
            .ToListAsync(ct);
        if (bloqueado.Count == 0) return ResultadoModeracionEvento.NoEncontrado;

        var evento = await _db.Eventos.FirstAsync(e => e.Id == m.EventoId, ct);
        if (evento.Estado != m.EstadoEsperado) return ResultadoModeracionEvento.YaRevisado;
        if (evento.Fin <= m.AhoraUtc) return ResultadoModeracionEvento.Finalizado;

        var antes = Foto(evento);
        if (m.Cambios is { } c)
        {
            evento.Titulo = c.Titulo;
            evento.Descripcion = c.Descripcion;
            evento.LugarId = c.LugarId;
            evento.LugarTexto = c.LugarTexto;
            evento.Inicio = c.InicioUtc;
            evento.Fin = c.FinUtc;
            evento.Asistencia = c.Asistencia;
            evento.VisitaMin = c.VisitaMin;
            evento.VisitaMax = c.VisitaMax;
        }
        evento.Estado = m.EstadoNuevo;
        evento.Motivo = m.EstadoNuevo == Evento.EstadoRechazado ? m.Motivo : null;
        evento.UpdatedAt = m.AhoraUtc;
        await _db.SaveChangesAsync(ct);

        // HU26: quién lo moderó y qué cambió, como los cambios de lugares del admin.
        var anteriores = JsonSerializer.Serialize(antes);
        var nuevos = JsonSerializer.Serialize(Foto(evento));
        await _db.Database.ExecuteSqlAsync($"""
                INSERT INTO historial_cambio (entidad, entidad_id, accion, valores_anteriores, valores_nuevos, usuario_id)
                VALUES ('EVENTO', {evento.Id}, 'UPDATE', CAST({anteriores} AS JSON), CAST({nuevos} AS JSON), {m.AdminId})
                """, ct);

        // uq_notif_evento (usuario, evento, tipo): si ya hubo un aviso igual, se reactiva con el texto nuevo.
        var mensaje = ReglasEvento.AvisoModeracion(m.TipoAviso, evento.Titulo, m.Motivo);
        await _db.Database.ExecuteSqlAsync($"""
                INSERT INTO notificacion (usuario_id, tipo, evento_id, mensaje, leida, activa, created_at)
                VALUES ({evento.CreadorId}, {m.TipoAviso}, {evento.Id}, {mensaje}, FALSE, TRUE, {m.AhoraUtc})
                ON DUPLICATE KEY UPDATE mensaje = VALUES(mensaje), leida = FALSE, activa = TRUE,
                                        created_at = VALUES(created_at)
                """, ct);
        await tx.CommitAsync(ct);
        return ResultadoModeracionEvento.Actualizado;
    }

    /// <summary>Lo que se guarda del evento en historial_cambio (sin la foto).</summary>
    private static Dictionary<string, object?> Foto(Evento e) => new()
    {
        ["estado"] = e.Estado,
        ["motivo"] = e.Motivo,
        ["titulo"] = e.Titulo,
        ["descripcion"] = e.Descripcion,
        ["lugar_id"] = e.LugarId,
        ["lugar_texto"] = e.LugarTexto,
        ["inicio"] = DateTime.SpecifyKind(e.Inicio, DateTimeKind.Utc),
        ["fin"] = DateTime.SpecifyKind(e.Fin, DateTimeKind.Utc),
        ["asistencia"] = e.Asistencia,
        ["visita_min"] = e.VisitaMin,
        ["visita_max"] = e.VisitaMax
    };

    private async Task<List<EventoListado>> ListarAsync(IQueryable<Evento> eventos, CancellationToken ct)
    {
        var filas = await eventos
            .Include(e => e.Lugar)
            .OrderByDescending(e => e.Inicio)
            .Select(e => new
            {
                Evento = e,
                NombreLugar = e.LugarId == null ? e.LugarTexto! : e.Lugar!.Nombre
            })
            .ToListAsync(ct);

        if (filas.Count == 0) return [];
        var ids = filas.Select(f => f.Evento.Id).ToList();
        var fotos = await _db.EventoImagenes.AsNoTracking()
            .Where(i => ids.Contains(i.EventoId))
            .Select(i => new { i.EventoId, i.ActualizadaEn })
            .ToDictionaryAsync(i => i.EventoId, i => (DateTime?)i.ActualizadaEn, ct);

        return filas.Select(f => new EventoListado(
            f.Evento,
            f.NombreLugar,
            fotos.GetValueOrDefault(f.Evento.Id))).ToList();
    }

    private Task<int> GuardarImagenAsync(int eventoId, byte[] contenido, string tipoMime, DateTime actualizadaEn, CancellationToken ct)
        => _db.Database.ExecuteSqlAsync($"""
                INSERT INTO evento_imagen (evento_id, contenido, tipo_mime, bytes, actualizada_en)
                VALUES ({eventoId}, {contenido}, {tipoMime}, {contenido.Length}, {ReglasEvento.VersionImagen(actualizadaEn)})
                ON DUPLICATE KEY UPDATE contenido = VALUES(contenido), tipo_mime = VALUES(tipo_mime),
                                        bytes = VALUES(bytes), actualizada_en = VALUES(actualizada_en)
                """, ct);

    private sealed class FilaFoto
    {
        public byte[] Contenido { get; set; } = [];
        public string TipoMime { get; set; } = "";
        public DateTime ActualizadaEn { get; set; }
        public int CreadorId { get; set; }
        public string Estado { get; set; } = "";
    }
}
