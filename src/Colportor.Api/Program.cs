using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Colportor.Api.Extensions;
using Colportor.Api.Data;
using Colportor.Api.Services;
using Colportor.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using Serilog;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Configurar serviços
builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddStructuredLogging(builder.Configuration);
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddRateLimiting();
builder.Services.AddCorsPolicy();
builder.Services.AddJwtAuthentication(builder.Configuration);

// Configurar controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Colportor API", Version = "v1" });
    
    // Configurar JWT no Swagger
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });
    
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configurar arquivos estáticos
builder.Services.Configure<IISServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});

var app = builder.Build();

// Configurar pipeline de request
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Rate limiting
app.UseIpRateLimiting();

// Cache middleware - temporariamente desabilitado
// app.UseCacheMiddleware();

// CORS
app.UseCors("AllowSpecificOrigins");

// Arquivos estáticos
app.UseDefaultFiles();
app.UseStaticFiles();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();

// Public registration endpoint (legacy compatibility)
app.MapPost("/leaders/register", async (IAuthService authService, RegisterDto registerDto) =>
{
    try
    {
        var result = await authService.RegisterAsync(registerDto);
        if (result.Success)
        {
            return Results.Ok(new { message = "Solicitação de líder enviada com sucesso. Aguarde aprovação." });
        }
        return Results.BadRequest(new { message = result.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao registrar líder: {ex.Message}");
    }
});

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));

// Configurar migrações automáticas (apenas em desenvolvimento)
if (app.Environment.IsDevelopment())
{
    // Executar migrações automaticamente com retry
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var maxRetries = 5;
    var delay = 2000; // 2 segundos

    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            await context.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (i < maxRetries - 1)
        {
            Console.WriteLine($"Tentativa {i + 1} de migração falhou: {ex.Message}. Aguardando {delay}ms...");
            await Task.Delay(delay);
            delay *= 2; // Exponential backoff
        }
    }

    // Executar seed data
    try
    {
        var seedService = scope.ServiceProvider.GetRequiredService<SeedDataService>();
        await seedService.SeedAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao executar seed data: {ex.Message}");
    }
}

Log.Information("Colportor API iniciada com sucesso");

app.Run();
