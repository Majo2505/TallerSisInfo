using Breaku.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Breaku.Infrastructure.Persistence;

public class NotificacionConfiguration : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> builder)
    {
        builder.ToTable("notificacion");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id).HasColumnName("id");
        builder.Property(n => n.UsuarioId).HasColumnName("usuario_id");
        builder.Property(n => n.Tipo).HasColumnName("tipo").HasMaxLength(25).IsRequired();
        builder.Property(n => n.ComunidadId).HasColumnName("comunidad_id");
        builder.Property(n => n.PuenteFecha).HasColumnName("puente_fecha").HasColumnType("date");
        builder.Property(n => n.PuenteHoraInicio).HasColumnName("puente_hora_inicio").HasColumnType("time");
        builder.Property(n => n.PuenteHoraFin).HasColumnName("puente_hora_fin").HasColumnType("time");
        builder.Property(n => n.Mensaje).HasColumnName("mensaje").HasMaxLength(200).IsRequired();
        builder.Property(n => n.Leida).HasColumnName("leida");
        builder.Property(n => n.Activa).HasColumnName("activa");
        builder.Property(n => n.CreatedAt).HasColumnName("created_at").HasColumnType("datetime(6)");

        builder.HasIndex(n => new { n.UsuarioId, n.PuenteFecha, n.PuenteHoraInicio }).IsUnique().HasDatabaseName("uq_notif_puente");
        builder.HasOne<Usuario>().WithMany().HasForeignKey(n => n.UsuarioId);
    }
}
