using Breaku.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Breaku.Infrastructure.Persistence;

/// <summary>Mapeo de lectura de la tabla lugar: la API no inserta ni modifica el catálogo.</summary>
public class LugarConfiguration : IEntityTypeConfiguration<Lugar>
{
    public void Configure(EntityTypeBuilder<Lugar> builder)
    {
        builder.ToTable("lugar");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        builder.Property(l => l.Descripcion).HasColumnName("descripcion");
        builder.Property(l => l.CategoriaId).HasColumnName("categoria_id");
        builder.Property(l => l.Latitud).HasColumnName("latitud").HasPrecision(9, 6);
        builder.Property(l => l.Longitud).HasColumnName("longitud").HasPrecision(9, 6);
        builder.Property(l => l.DuracionEstimadaMin).HasColumnName("duracion_estimada_min");
        builder.Property(l => l.Estado).HasColumnName("estado").HasMaxLength(15).IsRequired();

        builder.HasOne(l => l.Categoria).WithMany().HasForeignKey(l => l.CategoriaId);
    }
}
