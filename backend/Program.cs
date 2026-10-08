using System.Text;
using Breaku.Infrastructure;
using Breaku.Application.Horarios;
using Breaku.Infrastructure.Persistence;
using Breaku.Application.Puentes;
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
        "Falta la configuración 'FRONTEND_ORIGIN'. Definila en backend/.env (ver backend/.env.example).");
}

builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy =>
    policy.WithOrigins(frontendOrigin.TrimEnd('/')).AllowAnyHeader().AllowAnyMethod()));

// JWT: debe coincidir con lo que firma AuthService (clave, emisor y audiencia).
var jwtSecret = builder.Configuration["JWT_SECRET"];
if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException(
        "Falta la configuración 'JWT_SECRET' o es demasiado corta (mínimo 32 caracteres). Definila en backend/.env (ver backend/.env.example).");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
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
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IHorarioRepository, HorarioRepository>();
builder.Services.AddScoped<HorarioService>();
builder.Services.AddScoped<IPuenteRepository, PuenteRepository>();
builder.Services.AddScoped<PuenteService>();

var app = builder.Build();

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
