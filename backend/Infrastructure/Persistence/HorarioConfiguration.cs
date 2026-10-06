using Breaku.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Breaku.Infrastructure.Persistence;

public class HorarioConfiguration : IEntityTypeConfiguration<Horario>
{
    public void Configure(EntityTypeBuilder<Horario> builder)
    {
        builder.ToTable("horario");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id).HasColumnName("id");
        builder.Property(h => h.UsuarioId).HasColumnName("usuario_id");
        builder.Property(h => h.Materia).HasColumnName("materia").HasMaxLength(100).IsRequired();
        builder.Property(h => h.DiaSemana).HasColumnName("dia_semana");
        builder.Property(h => h.HoraInicio).HasColumnName("hora_inicio").HasColumnType("time");
        builder.Property(h => h.HoraFin).HasColumnName("hora_fin").HasColumnType("time");
        builder.Property(h => h.Aula).HasColumnName("aula").HasMaxLength(30);
        builder.Property(h => h.Origen).HasColumnName("origen").HasMaxLength(10).IsRequired();
        builder.Property(h => h.CreatedAt).HasColumnName("created_at").HasColumnType("datetime(6)")
            .HasDefaultValueSql("(UTC_TIMESTAMP(6))");
        builder.Property(h => h.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime(6)")
            .HasDefaultValueSql("(UTC_TIMESTAMP(6))");

        builder.HasOne<Usuario>().WithMany().HasForeignKey(h => h.UsuarioId);
    }
}
