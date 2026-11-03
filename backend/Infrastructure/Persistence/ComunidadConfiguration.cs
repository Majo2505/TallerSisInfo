using Breaku.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Breaku.Infrastructure.Persistence;

public sealed class ComunidadConfiguration : IEntityTypeConfiguration<ComunidadEfimera>
{
    public void Configure(EntityTypeBuilder<ComunidadEfimera> builder)
    {
        builder.ToTable("comunidad_efimera");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.CreadorId).HasColumnName("creador_id");
        builder.Property(c => c.LogId).HasColumnName("log_id");
        builder.Property(c => c.TextoOriginal).HasColumnName("texto_original").IsRequired();
        builder.Property(c => c.Actividad).HasColumnName("actividad").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Inicio).HasColumnName("inicio").HasColumnType("datetime(6)");
        builder.Property(c => c.Fin).HasColumnName("fin").HasColumnType("datetime(6)");
        builder.Property(c => c.MaxParticipantes).HasColumnName("max_participantes");
        builder.Property(c => c.LugarId).HasColumnName("lugar_id");
        builder.Property(c => c.Estado).HasColumnName("estado").HasMaxLength(10);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("datetime(6)");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime(6)");
    }
}
