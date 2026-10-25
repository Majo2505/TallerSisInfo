using Breaku.Domain;
using Microsoft.EntityFrameworkCore;

namespace Breaku.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Zona> Zonas => Set<Zona>();
    public DbSet<Horario> Horarios => Set<Horario>();
    public DbSet<Puente> Puentes => Set<Puente>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new HorarioConfiguration());
    }
}
