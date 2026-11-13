using Breaku.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Breaku.Infrastructure.Persistence;

public sealed class SancionConfiguration : IEntityTypeConfiguration<Sancion>
{
    public void Configure(EntityTypeBuilder<Sancion> builder)
    {
        builder.ToTable("sancion");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.UsuarioId).HasColumnName("usuario_id");
        builder.Property(s => s.Tipo).HasColumnName("tipo").HasMaxLength(12).IsRequired();
        builder.Property(s => s.Motivo).HasColumnName("motivo").HasMaxLength(500).IsRequired();
        builder.Property(s => s.ReporteId).HasColumnName("reporte_id");
        builder.Property(s => s.CreadoPor).HasColumnName("creado_por");
        builder.Property(s => s.CreadoEn).HasColumnName("creado_en").HasColumnType("datetime(6)");
        builder.Property(s => s.Hasta).HasColumnName("hasta").HasColumnType("datetime(6)");
        builder.Property(s => s.LevantadaEn).HasColumnName("levantada_en").HasColumnType("datetime(6)");
        builder.Property(s => s.LevantadaPor).HasColumnName("levantada_por");

        builder.HasIndex(s => new { s.UsuarioId, s.Tipo, s.Hasta }).HasDatabaseName("ix_sancion_usuario");
    }
}
