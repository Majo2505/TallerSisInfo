using Breaku.Application.Perfil;
using Breaku.Application.Admin;
using Breaku.Application.Reportes;
using Breaku.Application.Matching;
using System.Text;
using System.Threading.RateLimiting;
using Breaku.Application.DTOs;
using Breaku.Domain.Rules;
using Microsoft.AspNetCore.RateLimiting;
using Breaku.Infrastructure;
using Breaku.Application.Horarios;
using Breaku.Infrastructure.Persistence;
using Breaku.Application.Puentes;
using Breaku.Application.Notificaciones;
using Breaku.Application.Recomendaciones;
using Breaku.Presentation.Documentacion;
using Breaku.Application.Lugares;
using Breaku.Application.Ubicacion;
using Breaku.Application.Actividades;
using Breaku.Application.Comunidades;
using Breaku.Infrastructure.Performance;
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
        await contexto.HttpContext.Response.WriteAsJsonAsync(
            new ErrorNegocioDto(ReglasRendimiento.CodigoDemasiadasConsultas, ReglasRendimiento.MensajeDemasiadasConsultas), ct);
    };
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
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IHorarioRepository, HorarioRepository>();
builder.Services.AddScoped<HorarioService>();
builder.Services.AddScoped<FotoHorarioService>();
builder.Services.AddScoped<IPuenteRepository, PuenteRepository>();
builder.Services.AddScoped<PuenteService>();
builder.Services.AddScoped<NotificacionService>();
builder.Services.AddScoped<UbicacionService>();
builder.Services.AddScoped<PerfilService>();
builder.Services.AddScoped<MatchingService>();
builder.Services.AddScoped<BusquedaLugaresService>();
builder.Services.AddScoped<FotoLugarService>();
builder.Services.AddScoped<IRecuperadorLugares>(sp => sp.GetRequiredService<BusquedaLugaresService>());
builder.Services.AddScoped<RecomendacionService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<ReporteService>();
builder.Services.AddScoped<ExtraccionActividadService>();
builder.Services.AddScoped<ComunidadService>();
builder.Services.AddScoped<DescubrirComunidadesService>();
builder.Services.AddScoped<AsistenciaService>();
builder.Services.AddScoped(sp => new CreacionActividadService(
    sp.GetRequiredService<IActividadRepository>(), TimeProvider.System, sp.GetRequiredService<ComunidadService>()));

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
