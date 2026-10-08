using Breaku.Application;
using Breaku.Application.Common.Interfaces;
using Breaku.Infrastructure.Persistence;
using Breaku.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
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

        return services;
    }

    private static string Required(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Falta la configuración '{key}'. Definila en backend/.env (ver backend/.env.example).");
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