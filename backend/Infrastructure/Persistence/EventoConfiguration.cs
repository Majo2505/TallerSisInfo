using Breaku.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Breaku.Infrastructure.Persistence;

public sealed class EventoConfiguration : IEntityTypeConfiguration<EventoUniversitario>
{
    public void Configure(EntityTypeBuilder<EventoUniversitario> builder)
    {
        builder.ToTable("evento_universitario");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Fuente).HasColumnName("fuente").HasMaxLength(40).IsRequired();
        builder.Property(e => e.FuenteUrl).HasColumnName("fuente_url").HasMaxLength(500).IsRequired();
        builder.Property(e => e.Titulo).HasColumnName("titulo").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(2000).IsRequired();
        builder.Property(e => e.Inicio).HasColumnName("inicio").HasColumnType("datetime(6)");
        builder.Property(e => e.Fin).HasColumnName("fin").HasColumnType("datetime(6)");
        builder.Property(e => e.Lugar).HasColumnName("lugar").HasMaxLength(200);
        builder.Property(e => e.ActualizadoEn).HasColumnName("actualizado_en").HasColumnType("datetime(6)");
        builder.HasIndex(e => e.FuenteUrl).IsUnique();
        builder.HasIndex(e => new { e.Inicio, e.Fin });
    }
}
