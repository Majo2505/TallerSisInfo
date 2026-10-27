using Breaku.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Breaku.Infrastructure.Persistence;

public class LogAuditoriaRagConfiguration : IEntityTypeConfiguration<LogAuditoriaRag>
{
    public void Configure(EntityTypeBuilder<LogAuditoriaRag> builder)
    {
        builder.ToTable("log_auditoria_rag");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.UsuarioId).HasColumnName("usuario_id");
        builder.Property(l => l.Tipo).HasColumnName("tipo").HasMaxLength(25).IsRequired();
        builder.Property(l => l.ZonaId).HasColumnName("zona_id");
        builder.Property(l => l.RadioAplicadoM).HasColumnName("radio_aplicado_m");
        builder.Property(l => l.PuenteId).HasColumnName("puente_id");
        builder.Property(l => l.TiempoDisponibleMin).HasColumnName("tiempo_disponible_min");
        builder.Property(l => l.EntradaUsuario).HasColumnName("entrada_usuario").HasColumnType("text");
        builder.Property(l => l.PromptEnviado).HasColumnName("prompt_enviado").HasColumnType("text");
        builder.Property(l => l.RespuestaLlm).HasColumnName("respuesta_llm").HasColumnType("text");
        builder.Property(l => l.Estado).HasColumnName("estado").HasMaxLength(20).IsRequired();
        builder.Property(l => l.Modelo).HasColumnName("modelo").HasMaxLength(40);
        builder.Property(l => l.LatenciaMs).HasColumnName("latencia_ms");
        builder.Property(l => l.CreatedAt).HasColumnName("created_at").HasColumnType("datetime(6)");

        // zona_id y puente_id no se mapean como relación: zona la mapea HU5 y puente se borra al cambiar el horario
        // (en la base es ON DELETE SET NULL). Las FKs las sigue haciendo cumplir MySQL.
        builder.HasOne<Usuario>().WithMany().HasForeignKey(l => l.UsuarioId);
    }
}
