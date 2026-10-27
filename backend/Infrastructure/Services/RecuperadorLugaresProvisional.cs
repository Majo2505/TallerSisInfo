using Breaku.Application.Recomendaciones;
using Breaku.Domain;
using Breaku.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Services;

/// <summary>
/// PROVISIONAL: búsqueda mínima en el catálogo para poder probar HU7 de punta a punta.
/// Se reemplaza por la implementación de HU6 (y la zona del usuario de HU5) cambiando el registro en DependencyInjection.
/// Usa la primera zona registrada como campus de origen y lee el catálogo con SQL, sin mapear Lugar ni Zona,
/// para no chocar con las entidades que agregan HU5 y HU6.
/// </summary>
public class RecuperadorLugaresProvisional : IRecuperadorLugares
{
    private const double MetrosPorMinutoCaminando = 80;   // ~4,8 km/h
    private const int MaximoCandidatos = 10;

    private readonly AppDbContext _db;
    public RecuperadorLugaresProvisional(AppDbContext db) => _db = db;

    public async Task<ContextoRecuperado> RecuperarAsync(int usuarioId, Puente puente, CancellationToken ct = default)
    {
        var zona = (await _db.Database.SqlQuery<FilaZona>(
                $"SELECT id AS Id, latitud AS Latitud, longitud AS Longitud, radio_max_m AS RadioMaxM FROM zona ORDER BY id")
            .ToListAsync(ct)).FirstOrDefault();
        if (zona is null) return new ContextoRecuperado(null, null, []);

        // Solo lugares ACTIVO: verificados y aprobados por el administrador (HU6, criterio 5).
        var lugares = await _db.Database.SqlQuery<FilaLugar>($"""
                SELECT l.id AS Id, l.nombre AS Nombre, c.nombre AS Categoria, l.descripcion AS Descripcion,
                       l.latitud AS Latitud, l.longitud AS Longitud, l.duracion_estimada_min AS DuracionEstimadaMin
                FROM lugar l JOIN categoria c ON c.id = l.categoria_id
                WHERE l.estado = 'ACTIVO'
                """)
            .ToListAsync(ct);

        var disponible = (int)(puente.HoraFin - puente.HoraInicio).TotalMinutes;

        var candidatos = lugares
            .Select(l =>
            {
                var distancia = (int)Math.Round(DistanciaMetros(zona.Latitud, zona.Longitud, l.Latitud, l.Longitud));
                var traslado = (int)Math.Ceiling(2 * distancia / MetrosPorMinutoCaminando);
                return new LugarCandidato(l.Id, l.Nombre, l.Categoria, l.Descripcion, distancia, traslado, l.DuracionEstimadaMin);
            })
            .Where(c => c.DistanciaM <= zona.RadioMaxM)                                  // dentro del radio
            .Where(c => c.TiempoTrasladoMin + c.DuracionEstimadaMin <= disponible)       // ida + vuelta + actividad caben
            .OrderBy(c => c.DistanciaM)
            .Take(MaximoCandidatos)
            .ToList();

        return new ContextoRecuperado(zona.Id, zona.RadioMaxM, candidatos);
    }

    /// <summary>Distancia en línea recta (fórmula de haversine).</summary>
    private static double DistanciaMetros(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
    {
        const double RadioTierraM = 6_371_000;
        static double Rad(decimal grados) => (double)grados * Math.PI / 180;

        var dLat = Rad(lat2 - lat1);
        var dLng = Rad(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return 2 * RadioTierraM * Math.Asin(Math.Sqrt(a));
    }

    private sealed class FilaZona
    {
        public int Id { get; set; }
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
        public int RadioMaxM { get; set; }
    }

    private sealed class FilaLugar
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string? Descripcion { get; set; }
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
        public short DuracionEstimadaMin { get; set; }
    }
}
