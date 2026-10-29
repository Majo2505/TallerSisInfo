using Breaku.Application;
using Breaku.Application.Common.Interfaces;
using Breaku.Infrastructure.Persistence;
using Breaku.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using MySqlConnector;
using Breaku.Application.Horarios;
using Breaku.Application.Puentes;
using Breaku.Application.Notificaciones;
using Breaku.Application.Ubicacion;
using Breaku.Application.Recomendaciones;
using Breaku.Application.Admin;
namespace Breaku.Infrastructure;
public static class DependencyInjection
{
    private static readonly ServerVersion MySqlVersion = new MySqlServerVersion(new Version(8, 0, 46));

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = new MySqlConnectionStringBuilder
        {
            Server = Required(configuration, "DB_HOST"),
            Port = ParsePort(Required(configuration, "DB_PORT")),
            Database = Required(configuration, "DB_NAME"),
            UserID = Required(configuration, "DB_USER"),
            Password = Required(configuration, "DB_PASSWORD"),
            DateTimeKind = MySqlDateTimeKind.Utc
        }.ConnectionString;

        services.AddDbContext<AppDbContext>(options => options.UseMySql(connectionString, MySqlVersion));

        services.AddScoped<IDatabaseHealthCheck, DatabaseHealthCheck>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<IHorarioRepository, HorarioRepository>();
        services.AddScoped<IPuenteRepository, PuenteRepository>();
        services.AddScoped<INotificacionRepository, NotificacionRepository>();
        services.AddScoped<IRecomendacionRepository, RecomendacionRepository>();
        services.AddScoped<IAdminRepository, AdminRepository>();

        // HU7. PROVISIONAL: se cambia por la búsqueda de HU6 cuando esté lista.
        services.AddScoped<IRecuperadorLugares, RecuperadorLugaresProvisional>();
        AddAsistenteIa(services, configuration);

        services.AddHostedService<AvisoPuentesWorker>();
        services.AddScoped<IUbicacionRepository, UbicacionRepository>();

        return services;
    }

    /// <summary>
    /// HU7: con LLM_API_KEY se usa el LLM de LLM_BASE_URL (y LLM_BASE_URL y LLM_MODEL pasan a ser obligatorios).
    /// Sin clave, el asistente simulado, para que quien no tenga clave pueda levantar la API igual.
    /// HU2: la lectura de la foto del horario usa el mismo proveedor con LLM_MODEL_IMAGEN; sin clave no está disponible.
    /// </summary>
    private static void AddAsistenteIa(IServiceCollection services, IConfiguration configuration)
    {
        var apiKey = configuration["LLM_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            services.AddScoped<IAsistenteIa, AsistenteIaSimulado>();
            services.AddScoped<ILectorHorarioFoto, LectorHorarioNoDisponible>();
            return;
        }

        var urlBase = Required(configuration, "LLM_BASE_URL");
        if (!Uri.TryCreate(urlBase.TrimEnd('/') + "/", UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("La configuración 'LLM_BASE_URL' debe ser una URL absoluta (ejemplo para Ollama: http://localhost:11434/v1/).");
        }

        var modelo = Required(configuration, "LLM_MODEL");
        var segundos = AsistenteLlm.SegundosMaximosPorDefecto;
        var timeout = configuration["LLM_TIMEOUT_SECONDS"];
        if (!string.IsNullOrWhiteSpace(timeout) && (!int.TryParse(timeout, out segundos) || segundos is < 1 or > 600))
        {
            throw new InvalidOperationException("La configuración 'LLM_TIMEOUT_SECONDS' debe ser un número de segundos entre 1 y 600.");
        }

        services.AddHttpClient(AsistenteLlm.NombreCliente, http =>
        {
            http.BaseAddress = uri;   // con "/" final, para que "chat/completions" se agregue y no reemplace la ruta
            http.Timeout = TimeSpan.FromSeconds(segundos);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        });
        services.AddScoped<IAsistenteIa>(sp => new AsistenteLlm(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(AsistenteLlm.NombreCliente),
            modelo,
            sp.GetRequiredService<ILogger<AsistenteLlm>>()));

        // Foto del horario (HU2): mismo proveedor y clave; modelo de visión propio (LLM_MODEL_IMAGEN, si no, el de texto)
        // y más tiempo de espera, porque leer una imagen tarda más que una recomendación.
        var modeloImagen = configuration["LLM_MODEL_IMAGEN"];
        if (string.IsNullOrWhiteSpace(modeloImagen)) modeloImagen = modelo;
        services.AddHttpClient(LectorHorarioLlm.NombreCliente, http =>
        {
            http.BaseAddress = uri;
            http.Timeout = TimeSpan.FromSeconds(Math.Max(segundos, LectorHorarioLlm.SegundosMinimos));
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        });
        services.AddScoped<ILectorHorarioFoto>(sp => new LectorHorarioLlm(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(LectorHorarioLlm.NombreCliente),
            modeloImagen,
            sp.GetRequiredService<ILogger<LectorHorarioLlm>>()));
    }

    private static string Required(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Falta la configuración '{key}'. Defínela en backend/.env (ver backend/.env.example).");
        }

        return value;
    }

    private static uint ParsePort(string value)
    {
        if (!uint.TryParse(value, out var port) || port is 0 or > 65535)
        {
            throw new InvalidOperationException("La configuración 'DB_PORT' debe ser un puerto válido (1-65535).");
        }

        return port;
    }
}