using Breaku.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Breaku.Infrastructure.Persistence;

public class ZonaConfiguration : IEntityTypeConfiguration<Zona>
{
    public void Configure(EntityTypeBuilder<Zona> builder)
    {
        builder.ToTable("zona");
        builder.HasKey(z => z.Id);

        builder.Property(z => z.Id).HasColumnName("id");
        builder.Property(z => z.Nombre).HasColumnName("nombre").HasMaxLength(80).IsRequired();
        builder.Property(z => z.Latitud).HasColumnName("latitud").HasPrecision(9, 6);
        builder.Property(z => z.Longitud).HasColumnName("longitud").HasPrecision(9, 6);
        builder.Property(z => z.RadioMaxM).HasColumnName("radio_max_m");
        builder.Property(z => z.Activa).HasColumnName("activa");
    }
}
