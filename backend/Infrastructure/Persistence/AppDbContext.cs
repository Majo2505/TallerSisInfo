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
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Lugar> Lugares => Set<Lugar>();
    public DbSet<Horario> Horarios => Set<Horario>();
    public DbSet<Puente> Puentes => Set<Puente>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<LogAuditoriaRag> LogsAuditoriaRag => Set<LogAuditoriaRag>();
    public DbSet<LugarRecuperado> LugaresRecuperados => Set<LugarRecuperado>();
    public DbSet<Recomendacion> Recomendaciones => Set<Recomendacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new HorarioConfiguration());
    }
}
