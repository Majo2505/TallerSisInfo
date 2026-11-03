using Breaku.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Breaku.Infrastructure.Persistence;

public class ComunidadConfiguration : IEntityTypeConfiguration<ComunidadEfimera>
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
        builder.Property(c => c.Estado).HasColumnName("estado").HasMaxLength(10).IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("datetime(6)");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime(6)");

        builder.HasIndex(c => c.LogId).IsUnique().HasDatabaseName("uq_com_log");
        builder.HasOne<Usuario>().WithMany().HasForeignKey(c => c.CreadorId);
    }
}

public class ParticipacionConfiguration : IEntityTypeConfiguration<Participacion>
{
    public void Configure(EntityTypeBuilder<Participacion> builder)
    {
        builder.ToTable("participacion");
        builder.HasKey(p => new { p.ComunidadId, p.UsuarioId });

        builder.Property(p => p.ComunidadId).HasColumnName("comunidad_id");
        builder.Property(p => p.UsuarioId).HasColumnName("usuario_id");
        builder.Property(p => p.Estado).HasColumnName("estado").HasMaxLength(10).IsRequired();
        builder.Property(p => p.InvitadoEn).HasColumnName("invitado_en").HasColumnType("datetime(6)");
        builder.Property(p => p.RespondidoEn).HasColumnName("respondido_en").HasColumnType("datetime(6)");

        builder.HasOne<ComunidadEfimera>().WithMany().HasForeignKey(p => p.ComunidadId);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(p => p.UsuarioId);
    }
}
