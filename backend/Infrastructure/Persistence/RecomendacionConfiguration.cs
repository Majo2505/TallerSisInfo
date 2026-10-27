using Breaku.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Breaku.Infrastructure.Persistence;

public class RecomendacionConfiguration : IEntityTypeConfiguration<Recomendacion>
{
    public void Configure(EntityTypeBuilder<Recomendacion> builder)
    {
        builder.ToTable("recomendacion");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.LogId).HasColumnName("log_id");
        builder.Property(r => r.LugarId).HasColumnName("lugar_id");
        builder.Property(r => r.Explicacion).HasColumnName("explicacion").HasColumnType("text").IsRequired();
        builder.Property(r => r.Feedback).HasColumnName("feedback").HasMaxLength(8);
        builder.Property(r => r.FechaFeedback).HasColumnName("fecha_feedback").HasColumnType("datetime(6)");

        builder.HasIndex(r => new { r.LogId, r.LugarId }).IsUnique().HasDatabaseName("uq_reco_log_lugar");

        // fk_reco_recuperado: solo se recomienda un lugar recuperado en ESA misma consulta (RN04 / RNF06).
        builder.HasOne(r => r.LugarRecuperado).WithMany()
            .HasForeignKey(r => new { r.LogId, r.LugarId });
    }
}
