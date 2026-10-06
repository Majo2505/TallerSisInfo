using Breaku.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Breaku.Infrastructure.Persistence;

public class PuenteConfiguration : IEntityTypeConfiguration<Puente>
{
    public void Configure(EntityTypeBuilder<Puente> builder)
    {
        builder.ToTable("puente");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.UsuarioId).HasColumnName("usuario_id");
        builder.Property(p => p.DiaSemana).HasColumnName("dia_semana");
        builder.Property(p => p.HoraInicio).HasColumnName("hora_inicio").HasColumnType("time");
        builder.Property(p => p.HoraFin).HasColumnName("hora_fin").HasColumnType("time");

        builder.HasOne<Usuario>().WithMany().HasForeignKey(p => p.UsuarioId);
    }
}
