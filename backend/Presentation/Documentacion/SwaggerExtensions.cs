using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Breaku.Presentation.Documentacion;

/// <summary>
/// Documentación interactiva de la API (OpenAPI 3 + Swagger UI).
/// Documento JSON: <c>/swagger/v1/swagger.json</c>. Interfaz: <c>/swagger</c>.
/// Se publica solo en Development, o en cualquier entorno si SWAGGER_ENABLED=true.
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>Nombre del esquema de seguridad que usan las operaciones protegidas.</summary>
    internal const string EsquemaJwt = "Bearer";

    private const string Descripcion = """
        API de **BREAKU**: detecta *puentes* (huecos libres de **45 minutos o más** entre dos clases del mismo día).

        ### Cómo probar los endpoints protegidos
        1. Ejecuta `POST /api/auth/login` (o `POST /api/auth/register`) con un correo `@ucb.edu.bo`.
        2. Copia el valor de `token` de la respuesta.
        3. Pulsa **Authorize** (arriba a la derecha), pega el token (sin escribir `Bearer`) y confirma.
        4. Los endpoints con candado (`/api/horario`, `/api/puentes`) ya se pueden probar con **Try it out**.

        ### Convenciones
        - JSON en `camelCase`. Mensajes de error en español.
        - Horas de clase en formato 24 h. Se aceptan `HH:mm` y `HH:mm:ss`; la API responde siempre `HH:mm:ss`.
        - `diaSemana`: 1 = lunes … 6 = sábado.
        - El usuario siempre sale del token; nunca se envía en el cuerpo ni en la URL.
        - Los errores de autenticación responden `{ "message": "..." }` y los de horarios `{ "codigo": "...", "mensaje": "..." }`.
        - El token vence a las 24 horas: pasado ese tiempo los endpoints protegidos responden `401`.
        """;

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "BREAKU API",
                Version = "v1",
                Description = Descripcion,
                Contact = new OpenApiContact { Name = "Equipo BREAKU — SIS-227, UCB" }
            });

            // Comentarios /// de controladores, DTOs y ejemplos (archivo generado por <GenerateDocumentationFile>).
            var xml = Path.Combine(AppContext.BaseDirectory, "Breaku.xml");
            if (File.Exists(xml))
            {
                options.IncludeXmlComments(xml, includeControllerXmlComments: true);
            }

            options.AddSecurityDefinition(EsquemaJwt, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Token JWT obtenido en `POST /api/auth/login` o `POST /api/auth/register`. Pégalo sin la palabra `Bearer`."
            });
            options.OperationFilter<RequiereJwtOperationFilter>();
        });

        return services;
    }

    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        var habilitado = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("SWAGGER_ENABLED");
        if (!habilitado) return app;

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "BREAKU API v1");
            options.DocumentTitle = "BREAKU API — Documentación";
            options.DisplayRequestDuration();
            options.EnablePersistAuthorization(); // el token sobrevive a recargar la página
        });

        return app;
    }
}
