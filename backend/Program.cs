using Breaku.Infrastructure;
using Breaku.Application.Horarios;
using Breaku.Infrastructure.Persistence;

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

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IHorarioRepository, HorarioRepository>();
builder.Services.AddScoped<HorarioService>();

var app = builder.Build();

app.UseCors(FrontendCorsPolicy);

app.MapControllers();

app.Run();
