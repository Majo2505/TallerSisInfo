using Breaku.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var envFile = Path.Combine(builder.Environment.ContentRootPath, ".env");
if (File.Exists(envFile))
{
    DotNetEnv.Env.NoClobber().Load(envFile);
    builder.Configuration.AddEnvironmentVariables();
}

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapControllers();

app.Run();
