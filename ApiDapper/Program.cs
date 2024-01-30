using ApiDapper.Repositories;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configura o Serilog
builder.Host.UseSerilog((hostingContext, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(hostingContext.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/myapp-.txt", rollingInterval: RollingInterval.Day)
);
// Adiciona servi�os ao cont�iner.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

// Define os endpoints da API aqui diretamente.
app.MapControllers();

try
{
    Log.Information("Iniciando a aplica��o web.");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "A aplica��o falhou ao iniciar.");
}
finally
{
    Log.CloseAndFlush();
}
