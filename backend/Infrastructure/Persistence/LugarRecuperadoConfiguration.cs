using Breaku.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Breaku.Infrastructure.Persistence;

public class LugarRecuperadoConfiguration : IEntityTypeConfiguration<LugarRecuperado>
{
    public void Configure(EntityTypeBuilder<LugarRecuperado> builder)
    {
        builder.ToTable("lugar_recuperado");
        builder.HasKey(l => new { l.LogId, l.LugarId });

        builder.Property(l => l.LogId).HasColumnName("log_id");
        builder.Property(l => l.LugarId).HasColumnName("lugar_id");
        builder.Property(l => l.DistanciaM).HasColumnName("distancia_m");
        builder.Property(l => l.TiempoTrasladoMin).HasColumnName("tiempo_traslado_min");
        builder.Property(l => l.Orden).HasColumnName("orden");

        builder.HasIndex(l => new { l.LogId, l.Orden }).IsUnique().HasDatabaseName("uq_lr_orden");
        builder.HasOne(l => l.Log).WithMany(log => log.LugaresRecuperados).HasForeignKey(l => l.LogId);
        // lugar_id → lugar: la entidad Lugar la mapea HU6; la FK la hace cumplir MySQL.
    }
}
