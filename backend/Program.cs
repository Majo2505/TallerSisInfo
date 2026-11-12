using System.Text;
using Breaku.Application;
using Breaku.Application.TiempoReal;
using Breaku.Presentation.TiempoReal;
using System.Threading.RateLimiting;
using Breaku.Application.DTOs;
using Breaku.Domain.Rules;
using Microsoft.AspNetCore.RateLimiting;
using Breaku.Infrastructure;
using Breaku.Presentation.Documentacion;
using Breaku.Presentation.Rendimiento;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

var envFile = Path.Combine(builder.Environment.ContentRootPath, ".env");
if (File.Exists(envFile))
{
    DotNetEnv.Env.NoClobber().Load(envFile);
    builder.Configuration.AddEnvironmentVariables();
}

var frontendOrigin = builder.Configuration["FRONTEND_ORIGIN"];
if (string.IsNullOrWhiteSpace(frontendOrigin))
{
    throw new InvalidOperationException(
        "Falta la configuración 'FRONTEND_ORIGIN'. Defínela en backend/.env (ver backend/.env.example).");
}

builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy =>
    policy.WithOrigins(frontendOrigin.TrimEnd('/')).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var jwtSecret = builder.Configuration["JWT_SECRET"];
if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException(
        "Falta la configuración 'JWT_SECRET' o es demasiado corta (mínimo 32 caracteres). Defínela en backend/.env (ver backend/.env.example).");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    context.Token = token;
                return Task.CompletedTask;
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JWT_ISSUER"] ?? "BreakuApi",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JWT_AUDIENCE"] ?? "BreakuApp",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddSignalR();
// HU19: en horas pico, cada estudiante tiene un límite de consultas por minuto a lo que usa la IA (recomendaciones,
// extracción y foto del horario). Protege la latencia de todos y los tokens del equipo.
builder.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opciones.OnRejected = async (contexto, ct) =>
    {
        if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
            contexto.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(espera.TotalSeconds)).ToString();
        var politica = contexto.HttpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
        var mensaje = politica == ReglasRendimiento.PoliticaIa
            ? ReglasRendimiento.MensajeDemasiadasConsultas
            : ReglasCuenta.MensajeDemasiadosIntentos;
        await contexto.HttpContext.Response.WriteAsJsonAsync(new ErrorNegocioDto(ReglasRendimiento.CodigoDemasiadasConsultas, mensaje), ct);
    };
    // Contraseñas y login: contra quien intenta adivinar. El login va por IP (todavía no hay usuario) con un margen alto,
    // porque en el campus muchos estudiantes salen por la misma IP; cambiar la contraseña va por estudiante.
    opciones.AddPolicy(ReglasCuenta.PoliticaLogin, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = ReglasCuenta.IntentosLoginPorMinuto, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    opciones.AddPolicy(ReglasCuenta.PoliticaContrasena, http => RateLimitPartition.GetFixedWindowLimiter(
        Breaku.Api.Hubs.UsuarioIdProvider.Leer(http.User)?.ToString() ?? http.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = ReglasCuenta.IntentosContrasenaPorMinuto, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    opciones.AddPolicy(ReglasRendimiento.PoliticaIa, http => RateLimitPartition.GetFixedWindowLimiter(
        Breaku.Api.Hubs.UsuarioIdProvider.Leer(http.User)?.ToString() ?? http.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = ReglasRendimiento.ConsultasIaPorMinuto,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, Breaku.Api.Hubs.UsuarioIdProvider>();
builder.Services.AddSingleton<ApiMetrics>();
builder.Services.AddSwaggerDocumentation();
// HU20: cada capa registra lo suyo. Application: casos de uso; Infrastructure: base de datos, IA y workers;
// Presentation (acá): controladores, hub y canal en vivo.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<ICanalEnVivo, CanalSignalR>();

var app = builder.Build();

app.UseMiddleware<ApiMetricsMiddleware>();
app.UseCors(FrontendCorsPolicy);

app.UseSwaggerDocumentation();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHub<Breaku.Api.Hubs.ComunidadHub>("/hubs/comunidades");

app.Run();
