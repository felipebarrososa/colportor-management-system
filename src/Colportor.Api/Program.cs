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

// Aplicar migrations automaticamente
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        // Primeiro, verificar quais tabelas existem e marcar migrations correspondentes
        Log.Information("Verificando tabelas existentes e corrigindo histórico de migrations");
        
        // Verificar tabelas existentes
        var existingTables = new List<string>();
        try
        {
            var tables = context.Database.ExecuteSqlRaw(
                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'");
            
            // Verificar tabelas específicas
            var tableChecks = new[]
            {
                "Countries", "Leaders", "Colportors", "Regions", "Photos", 
                "MissionContacts", "ContactObservations", "WhatsAppConnections", 
                "WhatsAppMessages", "WhatsAppTemplates", "Reminders"
            };
            
            foreach (var table in tableChecks)
            {
                try
                {
                    var exists = context.Database.ExecuteSqlRaw(
                        "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = {0})", table) > 0;
                    if (exists)
                    {
                        existingTables.Add(table);
                        Log.Information("Tabela {Table} existe", table);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Erro ao verificar tabela {Table}", table);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Erro ao verificar tabelas existentes");
        }
        
        // Marcar migrations baseado nas tabelas existentes
        var migrationsToMark = new Dictionary<string, string[]>
        {
            ["202409290001_Initial"] = new[] { "Countries", "Leaders", "Colportors", "Regions" },
            ["20251001124027_AddLeaderPersonalData"] = new[] { "Countries" },
            ["20251001193944_AddPacEnrollmentsToColportor"] = new[] { "Colportors" },
            ["20251001212504_FixColportorLeaderRelationship"] = new[] { "Colportors", "Leaders" },
            ["20251001213603_RemoveColportorCountryProperties"] = new[] { "Colportors" },
            ["20251002124656_AddGenderAndBirthDateToColportors"] = new[] { "Colportors" },
            ["20251002130658_AddGenderBirthDateSimple"] = new[] { "Colportors" },
            ["20251013220000_AddPhotosTable"] = new[] { "Photos" },
            ["20251020134639_AddMissionContacts"] = new[] { "MissionContacts", "ContactObservations" }
        };
        
        foreach (var migration in migrationsToMark)
        {
            try
            {
                // Verificar se todas as tabelas da migration existem
                var allTablesExist = migration.Value.All(table => existingTables.Contains(table));
                
                if (allTablesExist)
                {
                    Log.Information("Marcando migration {Migration} como aplicada (todas as tabelas existem)", migration.Key);
                    context.Database.ExecuteSqlRaw(
                        "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1}) ON CONFLICT DO NOTHING",
                        migration.Key, "8.0.8");
                }
                else
                {
                    Log.Information("Migration {Migration} não será marcada como aplicada (faltam tabelas)", migration.Key);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Erro ao marcar migration {Migration} como aplicada", migration.Key);
            }
        }
        
        // Agora aplicar migrations pendentes (só WhatsApp e Reminders)
        var pendingMigrations = context.Database.GetPendingMigrations();
        if (pendingMigrations.Any())
        {
            Log.Information("Aplicando {Count} migrations pendentes: {Migrations}", 
                pendingMigrations.Count(), string.Join(", ", pendingMigrations));
            
            try
            {
                context.Database.Migrate();
                Log.Information("Migrations aplicadas com sucesso");
            }
            catch (Exception migrationEx)
            {
                Log.Error(migrationEx, "Erro ao aplicar migrations pendentes, mas continuando");
            }
        }
        else
        {
            Log.Information("Nenhuma migration pendente encontrada");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Erro geral ao aplicar migrations - continuando sem falhar a aplicação");
        // Não falhar a aplicação se migrations falharem
    }
}

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
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Evitar cache para arquivos JavaScript e CSS
        if (ctx.File.Name.EndsWith(".js") || ctx.File.Name.EndsWith(".css"))
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            ctx.Context.Response.Headers.Append("Pragma", "no-cache");
            ctx.Context.Response.Headers.Append("Expires", "0");
        }
    }
});

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
