using Breaku.Application.Actividades;
using Breaku.Application.Admin;
using Breaku.Application.Comunidades;
using Breaku.Application.Horarios;
using Breaku.Application.Lugares;
using Breaku.Application.Matching;
using Breaku.Application.Notificaciones;
using Breaku.Application.Perfil;
using Breaku.Application.Puentes;
using Breaku.Application.Recomendaciones;
using Breaku.Application.Reportes;
using Breaku.Application.Sanciones;
using Breaku.Application.TiempoReal;
using Breaku.Application.Ubicacion;
using Microsoft.Extensions.DependencyInjection;

namespace Breaku.Application;

/// <summary>
/// HU20: los casos de uso de cada módulo se registran acá, igual que <c>AddInfrastructure</c> registra repositorios y
/// servicios externos. Para sumar un módulo: su servicio acá, su repositorio en Infrastructure y su controlador en
/// Presentation; <c>Program.cs</c> no cambia.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Horario, puentes y avisos (HU2, HU3, HU4)
        services.AddScoped<HorarioService>();
        services.AddScoped<FotoHorarioService>();
        services.AddScoped<PuenteService>();
        services.AddScoped<NotificacionService>();

        // Ubicación, perfil y matching (HU5, HU21, HU12)
        services.AddScoped<UbicacionService>();
        services.AddScoped<PerfilService>();
        services.AddScoped<MatchingService>();

        // Lugares y recomendaciones (HU6, HU7)
        services.AddScoped<BusquedaLugaresService>();
        services.AddScoped<IRecuperadorLugares>(sp => sp.GetRequiredService<BusquedaLugaresService>());
        services.AddScoped<RecomendacionService>();
        services.AddScoped<FotoLugarService>();

        // Admin y reportes
        services.AddScoped<AdminService>();
        services.AddScoped<ReporteService>();

        // Actividades y comunidades (HU10, HU11, HU13, HU14, HU15)
        services.AddScoped<ExtraccionActividadService>();
        services.AddScoped<ComunidadService>();
        services.AddScoped<IInvitadorComunidad>(sp => sp.GetRequiredService<ComunidadService>());
        services.AddScoped(sp => new CreacionActividadService(
            sp.GetRequiredService<IActividadRepository>(), TimeProvider.System, sp.GetRequiredService<IInvitadorComunidad>()));
        services.AddScoped<DescubrirComunidadesService>();
        services.AddScoped<AsistenciaService>();
        services.AddScoped<ICierreComunidadesService, CierreComunidadesService>();

        // Sanciones y baja de usuarios (HU28); la purga la reutiliza HU29
        services.AddScoped(sp => new PurgaUsuarioService(sp.GetRequiredService<IPurgaRepository>(), sp.GetRequiredService<ITransaccion>(),
            sp.GetRequiredService<ITiempoReal>(), TimeProvider.System));
        services.AddScoped(sp => new SancionService(sp.GetRequiredService<ISancionRepository>(), sp.GetRequiredService<PurgaUsuarioService>(),
            sp.GetRequiredService<ITransaccion>(), sp.GetRequiredService<ITiempoReal>(), sp.GetRequiredService<IValidadorSesion>(), TimeProvider.System));
        // Un solo validador para todo el proceso: guarda ~60 s lo que lee de la base, con un alcance propio por lectura.
        services.AddSingleton<IValidadorSesion>(sp => new ValidadorSesion(async (usuarioId, ahora, ct) =>
        {
            using var alcance = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
            return await alcance.ServiceProvider.GetRequiredService<ISancionRepository>().ObtenerEstadoSesionAsync(usuarioId, ahora, ct);
        }, TimeProvider.System));

        // Tiempo real (HU18): a quién avisar. El canal (SignalR) lo registra Presentation.
        services.AddScoped<ITiempoReal, TiempoRealService>();

        return services;
    }
}
