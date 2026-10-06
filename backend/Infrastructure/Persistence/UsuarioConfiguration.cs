using Breaku.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Breaku.Infrastructure.Persistence;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuario");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.Correo).HasColumnName("correo").HasMaxLength(120).IsRequired();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
        builder.Property(u => u.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        builder.Property(u => u.Rol).HasColumnName("rol").HasMaxLength(15).IsRequired();
        builder.Property(u => u.CorreoVerificado).HasColumnName("correo_verificado");
        builder.Property(u => u.ConsentimientoUbicacion).HasColumnName("consentimiento_ubicacion");
        builder.Property(u => u.FechaConsentimiento).HasColumnName("fecha_consentimiento").HasColumnType("datetime(6)");
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").HasColumnType("datetime(6)")
            .HasDefaultValueSql("(UTC_TIMESTAMP(6))");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime(6)")
            .HasDefaultValueSql("(UTC_TIMESTAMP(6))");
        builder.Property(u => u.UpdatedBy).HasColumnName("updated_by");
        builder.Property(u => u.Activo).HasColumnName("activo");

        builder.HasOne<Usuario>().WithMany().HasForeignKey(u => u.UpdatedBy);
    }
}
