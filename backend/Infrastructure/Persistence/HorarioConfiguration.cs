using Breaku.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Breaku.Infrastructure.Persistence;

/// <summary>Mapeo a la tabla horario del modelo v2. En OnModelCreating: modelBuilder.ApplyConfiguration(new HorarioConfiguration());</summary>
public class HorarioConfiguration : IEntityTypeConfiguration<Horario>
{
    public void Configure(EntityTypeBuilder<Horario> b)
    {
        b.ToTable("horario");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        b.Property(x => x.Materia).HasColumnName("materia").HasMaxLength(100).IsRequired();
        b.Property(x => x.DiaSemana).HasColumnName("dia_semana");
        b.Property(x => x.HoraInicio).HasColumnName("hora_inicio");
        b.Property(x => x.HoraFin).HasColumnName("hora_fin");
        b.Property(x => x.Aula).HasColumnName("aula").HasMaxLength(30);
        b.Property(x => x.Origen).HasColumnName("origen").HasMaxLength(10).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(x => new { x.UsuarioId, x.DiaSemana, x.HoraInicio }).IsUnique().HasDatabaseName("uq_horario_slot");
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_horario_dia", "dia_semana BETWEEN 1 AND 6");
            t.HasCheckConstraint("ck_horario_horas", "hora_fin > hora_inicio");
            t.HasCheckConstraint("ck_horario_origen", "origen IN ('MANUAL','FOTO')");
        });
        // FK a usuario: la define la entidad Usuario de HU1-DB (Ariana).
    }
}
