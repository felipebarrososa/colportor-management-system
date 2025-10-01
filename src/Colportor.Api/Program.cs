using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;

using Colportor.Api.Data;
using Colportor.Api.Services;
using Colportor.Api.Models;
using DTOsNS = Colportor.Api.DTOs;

// Aliases para evitar colisão com o namespace "Colportor"
using ColpUser = Colportor.Api.Models.User;
using ColpColportor = Colportor.Api.Models.Colportor;
using ColpVisit = Colportor.Api.Models.Visit;

var builder = WebApplication.CreateBuilder(args);

// DB - Sistema de connection string organizado por ambiente
string connectionString;

// Debug: Log todas as variáveis do Railway
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"DATABASE_URL: {(string.IsNullOrEmpty(builder.Configuration["DATABASE_URL"]) ? "NULL" : "EXISTS")}");
Console.WriteLine($"JWT_KEY: {(string.IsNullOrEmpty(builder.Configuration["JWT_KEY"]) ? "NULL" : "EXISTS")}");
Console.WriteLine($"JWT_KEY length: {(builder.Configuration["JWT_KEY"]?.Length ?? 0)}");
Console.WriteLine($"PORT: {builder.Configuration["PORT"]}");
Console.WriteLine($"ASPNETCORE_URLS: {builder.Configuration["ASPNETCORE_URLS"]}");
Console.WriteLine($"PGHOST: {builder.Configuration["PGHOST"]}");
Console.WriteLine($"PGPORT: {builder.Configuration["PGPORT"]}");
Console.WriteLine($"PGDATABASE: {builder.Configuration["PGDATABASE"]}");
Console.WriteLine($"PGUSER: {builder.Configuration["PGUSER"]}");
Console.WriteLine($"PGPASSWORD: {(string.IsNullOrEmpty(builder.Configuration["PGPASSWORD"]) ? "NULL" : "***")}");

// Debug: Listar todas as variáveis de ambiente que começam com PG ou DATABASE
var envVars = Environment.GetEnvironmentVariables();
Console.WriteLine("Environment variables starting with PG or DATABASE:");
foreach (System.Collections.DictionaryEntry envVar in envVars)
{
    var envKey = envVar.Key?.ToString();
    if (envKey != null && (envKey.StartsWith("PG") || envKey.StartsWith("DATABASE")))
    {
        var envValue = envVar.Value?.ToString();
        var safeValue = envKey.Contains("PASSWORD") ? "***" : envValue;
        Console.WriteLine($"  {envKey}: {safeValue}");
    }
}

// Debug: Listar TODAS as variáveis de ambiente para debug
Console.WriteLine("All environment variables:");
foreach (System.Collections.DictionaryEntry envVar in envVars)
{
    var envKey = envVar.Key?.ToString();
    if (envKey != null && envKey.Contains("DATABASE"))
    {
        var envValue = envVar.Value?.ToString();
        var safeValue = envKey.Contains("PASSWORD") ? "***" : envValue;
        Console.WriteLine($"  {envKey}: {safeValue}");
    }
}

// Lógica de connection string por ambiente
if (builder.Environment.IsProduction())
{
    // PRODUÇÃO: Usar DATABASE_URL do Railway
    var databaseUrl = builder.Configuration["DATABASE_URL"];
    Console.WriteLine($"DATABASE_URL exists: {!string.IsNullOrEmpty(databaseUrl)}");
    
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        // Converter DATABASE_URL para formato Npgsql
        if (databaseUrl.StartsWith("postgresql://"))
        {
            // Parse da URL: postgresql://user:password@host:port/database
            var uri = new Uri(databaseUrl);
            var host = uri.Host;
            var port = uri.Port;
            var database = uri.AbsolutePath.TrimStart('/');
            var username = uri.UserInfo.Split(':')[0];
            var password = uri.UserInfo.Split(':')[1];
            
            connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";
            Console.WriteLine("PRODUCTION: Using DATABASE_URL (converted to Npgsql format)");
        }
        else
        {
            connectionString = databaseUrl;
            Console.WriteLine("PRODUCTION: Using DATABASE_URL (direct)");
        }
    }
    else
    {
        // Fallback para variáveis individuais do Railway
        var pgHost = builder.Configuration["PGHOST"];
        var pgPort = builder.Configuration["PGPORT"];
        var pgDatabase = builder.Configuration["PGDATABASE"];
        var pgUser = builder.Configuration["PGUSER"];
        var pgPassword = builder.Configuration["PGPASSWORD"];

        if (!string.IsNullOrEmpty(pgHost) && !string.IsNullOrEmpty(pgDatabase))
        {
            connectionString = $"Host={pgHost};Port={pgPort};Database={pgDatabase};Username={pgUser};Password={pgPassword}";
            Console.WriteLine("PRODUCTION: Using Railway individual variables");
        }
        else
        {
            // TEMPORÁRIO: Usar connection string hardcoded para Railway
            // Substitua pelos valores reais do seu banco Railway
            connectionString = "postgresql://postgres:RNPzVecUtMNYkYRidaREmveXIZZHKsGD@postgres.railway.internal:5432/railway";
            Console.WriteLine("PRODUCTION: Using hardcoded Railway connection string");
            Console.WriteLine("⚠️  WARNING: This is a temporary solution! Please configure Railway properly.");
        }
    }
}
else
{
    // DESENVOLVIMENTO: Usar appsettings.Development.json
    connectionString = builder.Configuration.GetConnectionString("Default") 
        ?? throw new InvalidOperationException("Development database connection not found");
    Console.WriteLine("DEVELOPMENT: Using appsettings.Development.json");
}

// Debug: Log da connection string (sem senha)
var safeConnectionString = connectionString.Contains("@") 
    ? connectionString.Substring(0, connectionString.IndexOf("@")) + "@***" 
    : connectionString;
Console.WriteLine($"Using connection string: {safeConnectionString}");

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(connectionString));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Serviços
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<EmailService>();

// JSON - ignora ciclos se alguém esquecer de projetar DTO
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// Auth
var jwtKey = builder.Configuration["JWT_KEY"] ?? builder.Configuration["Jwt:Key"] ?? "dev_key_change_me_please_change";
var key = Encoding.UTF8.GetBytes(jwtKey);
Console.WriteLine($"JWT Key length: {key.Length}");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Static files (wwwroot)
app.UseDefaultFiles();
app.UseStaticFiles();

// Static files para uploads
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

#region Migrate & Seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!db.Database.GetMigrations().Any())
        db.Database.EnsureCreated();
    else
        db.Database.Migrate();

    // País / Regiões iniciais
    if (!db.Countries.Any())
    {
        var br = new Country { Name = "Brasil" };
        db.Countries.Add(br);
        db.Regions.AddRange(
            new Region { Name = "Norte", Country = br },
            new Region { Name = "Nordeste", Country = br },
            new Region { Name = "Centro-Oeste", Country = br },
            new Region { Name = "Sudeste", Country = br },
            new Region { Name = "Sul", Country = br }
        );
        db.SaveChanges();
    }

    // Admin master
    if (!db.Users.Any(u => u.Role == "Admin"))
    {
        Console.WriteLine("🔧 Creating admin user...");
        var admin = new ColpUser
        {
            Email = "admin@colportor.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = "Admin"
        };
        db.Users.Add(admin);
        db.SaveChanges();
        Console.WriteLine("✅ Admin user created successfully!");
    }
    else
    {
        Console.WriteLine("✅ Admin user already exists");
        var adminUser = db.Users.FirstOrDefault(u => u.Role == "Admin");
        Console.WriteLine($"   Email: {adminUser?.Email}");
    }
}
#endregion

// Helpers
static int? CurrentUserId(HttpContext ctx)
{
    var s = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
    return int.TryParse(s, out var id) ? id : null;
}

static async Task<ColpUser?> CurrentUserAsync(AppDbContext db, HttpContext ctx)
{
    var id = CurrentUserId(ctx);
    return id is null ? null : await db.Users.FindAsync(id.Value);
}

// ========= AUTH =========
app.MapPost("/auth/login", (AppDbContext db, JwtService jwt, LoginDto dto) =>
{
    Console.WriteLine($"🔐 Login attempt - Email: {dto.Email}");
    var user = db.Users.SingleOrDefault(u => u.Email == dto.Email);
    
    if (user is null)
    {
        Console.WriteLine($"❌ User not found: {dto.Email}");
        return Results.Unauthorized();
    }
    
    Console.WriteLine($"✅ User found: {user.Email}, Role: {user.Role}");
    
    var passwordMatch = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
    Console.WriteLine($"🔑 Password match: {passwordMatch}");
    
    if (!passwordMatch)
    {
        Console.WriteLine($"❌ Invalid password for: {dto.Email}");
        return Results.Unauthorized();
    }

    var token = jwt.GenerateToken(user); // se possível, faça este service incluir Role e (se tiver) RegionId no token
    Console.WriteLine($"✅ Login successful for: {dto.Email}");
    return Results.Ok(new TokenDto(token));
});

// Colportor se cadastra (carteira)
app.MapPost("/auth/register", async (AppDbContext db, DTOsNS.CreateColportorDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        return Results.BadRequest("Email e senha são obrigatórios.");
    if (await db.Users.AnyAsync(u => u.Email == dto.Email))
        return Results.BadRequest("E-mail já cadastrado.");
    if (await db.Colportors.AnyAsync(c => c.CPF == dto.CPF))
        return Results.BadRequest("CPF já cadastrado.");

    // cria colportor
    var colp = new ColpColportor
    {
        FullName = dto.FullName.Trim(),
        CPF = dto.CPF.Trim(),
        City = dto.City?.Trim(),
        PhotoUrl = dto.PhotoUrl,
        RegionId = dto.RegionId,
        LeaderId = dto.LeaderId
    };
    db.Colportors.Add(colp);
    await db.SaveChangesAsync();

    // Se a última visita for informada, normaliza para meia-noite UTC e cria registro
    if (dto.LastVisitDate is DateTime lv)
    {
        var d = lv.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(lv, DateTimeKind.Utc)
            : lv.ToUniversalTime();
        d = new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Utc);
        colp.LastVisitDate = d;
        db.Visits.Add(new ColpVisit { ColportorId = colp.Id, Date = d });
        await db.SaveChangesAsync();
    }

    // cria usuário (perfil de carteira)
    var user = new ColpUser
    {
        Email = dto.Email.Trim().ToLowerInvariant(),
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        Role = "Colportor",
        ColportorId = colp.Id
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/auth/register/{colp.Id}", new { colp.Id, dto.Email });
});

// ========= LEADER SELF-REGISTER =========
// Cadastro público de líder: cria usuário com Role="LeaderPending" aguardando aprovação
app.MapPost("/leaders/register", async (AppDbContext db, LeaderRegisterDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.CPF))
        return Results.BadRequest("Nome completo e CPF são obrigatórios.");
    if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        return Results.BadRequest("Email e senha são obrigatórios.");
    if (!await db.Regions.AnyAsync(r => r.Id == dto.RegionId))
        return Results.BadRequest("Região inválida.");
    if (await db.Users.AnyAsync(u => u.Email == dto.Email))
        return Results.Conflict("E-mail já em uso.");
    if (await db.Users.AnyAsync(u => u.CPF == dto.CPF))
        return Results.Conflict("CPF já cadastrado.");

    var user = new ColpUser
    {
        FullName = dto.FullName.Trim(),
        CPF = dto.CPF.Trim(),
        City = dto.City?.Trim(),
        Email = dto.Email.Trim().ToLowerInvariant(),
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        Role = "LeaderPending",
        RegionId = dto.RegionId
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/leaders/{user.Id}", new { user.Id, user.FullName, user.Email, user.RegionId, Status = user.Role });
});

// ========= GEO / UPLOAD =========

// Upload de foto (form-data: photo)
app.MapPost("/upload/photo", async (HttpRequest req) =>
{
    if (!req.HasFormContentType) return Results.BadRequest("form-data esperado");
    var file = req.Form.Files.GetFile("photo");
    if (file is null || file.Length == 0) return Results.BadRequest("arquivo vazio");

    var ext = Path.GetExtension(file.FileName);
    var name = $"{Guid.NewGuid():N}{ext}";
    var saveTo = Path.Combine(uploadsPath, name);

    await using var fs = File.Create(saveTo);
    await file.CopyToAsync(fs);

    var url = $"/uploads/{name}";
    return Results.Ok(new { url });
});

// Países e Regiões (para preencher selects)
app.MapGet("/geo/countries", (AppDbContext db) =>
    db.Countries.Select(c => new { c.Id, c.Name }).OrderBy(c => c.Name));

app.MapGet("/geo/regions", (AppDbContext db, int countryId) =>
    db.Regions.Where(r => r.CountryId == countryId)
              .Select(r => new { r.Id, r.Name })
              .OrderBy(r => r.Name));

// Listar líderes por região (público para cadastro)
app.MapGet("/geo/leaders", async (AppDbContext db, int? regionId) =>
{
    if (regionId == null)
        return Results.BadRequest("regionId é obrigatório");
    
    var leaders = await db.Users
        .Where(u => u.Role == "Leader" && u.RegionId == regionId)
        .Select(u => new { 
            u.Id, 
            u.Email, 
            Name = u.FullName ?? u.Email,
            u.CPF
        })
        .OrderBy(u => u.Name)
        .ToListAsync();
    
    Console.WriteLine($"📋 Retornando {leaders.Count} líderes para região {regionId}");
    return Results.Ok(leaders);
});

// ========= ADMIN =========

// Admin cria colportor (com região/líder)
app.MapPost("/admin/colportors", async (AppDbContext db, DTOsNS.CreateColportorDto dto) =>
{
    if (await db.Colportors.AnyAsync(c => c.CPF == dto.CPF))
        return Results.BadRequest("CPF já cadastrado.");

    var colp = new ColpColportor
    {
        FullName = dto.FullName.Trim(),
        CPF = dto.CPF.Trim(),
        City = dto.City?.Trim(),
        PhotoUrl = dto.PhotoUrl,
        RegionId = dto.RegionId,
        LeaderId = dto.LeaderId // Líder informado pelo frontend
    };
    
    if (dto.LeaderId != null)
    {
        Console.WriteLine($"✅ Colportor vinculado ao líder ID: {dto.LeaderId}");
    }
    
    db.Colportors.Add(colp);
    await db.SaveChangesAsync();

    // Se a última visita vier no payload do admin, também cria imediatamente
    if (dto.LastVisitDate is DateTime alv)
    {
        var d = alv.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(alv, DateTimeKind.Utc)
            : alv.ToUniversalTime();
        d = new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Utc);
        colp.LastVisitDate = d;
        db.Visits.Add(new ColpVisit { ColportorId = colp.Id, Date = d });
        await db.SaveChangesAsync();
    }

    // cria user para carteira
    var user = new ColpUser
    {
        Email = dto.Email.Trim().ToLowerInvariant(),
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        Role = "Colportor",
        ColportorId = colp.Id
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/admin/colportors/{colp.Id}", new { colp.Id });
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// Listar colportores (Admin vê tudo; Leader só os seus vinculados)
app.MapGet("/admin/colportors", async (AppDbContext db, HttpContext ctx, string? city, string? cpf, string? status) =>
{
    var user = await CurrentUserAsync(db, ctx);
    if (user is null) return Results.Unauthorized();

    var q = db.Colportors.Include(c => c.Region).Include(c => c.Visits).AsQueryable();

    if (user.Role == "Leader")
    {
        // Líder vê apenas colportores vinculados a ele
        q = q.Where(c => c.LeaderId == user.Id);
        Console.WriteLine($"🔍 Leader {user.Email} filtering colportors by LeaderId: {user.Id}");
    }
    else if (user.Role != "Admin")
    {
        // Outros roles não têm acesso
        return Results.Forbid();
    }
    else
    {
        Console.WriteLine($"👑 Admin {user.Email} viewing all colportors");
    }

    if (!string.IsNullOrWhiteSpace(city)) q = q.Where(c => (c.City ?? "").ToLower().Contains(city.ToLower()));
    if (!string.IsNullOrWhiteSpace(cpf)) q = q.Where(c => c.CPF.Contains(cpf));

    var list = await q.OrderBy(c => c.FullName).ToListAsync();

    var projected = list.Select(c =>
    {
        var (s, due) = StatusService.ComputeStatus(c.LastVisitDate);
        return new
        {
            c.Id,
            c.FullName,
            c.CPF,
            Region = c.Region != null ? c.Region.Name : null,
            c.City,
            c.PhotoUrl,
            c.LastVisitDate,
            Status = s,
            DueDate = due
        };
    });

    if (!string.IsNullOrWhiteSpace(status))
        projected = projected.Where(x => string.Equals(x.Status, status, StringComparison.OrdinalIgnoreCase));

    return Results.Ok(projected);
}).RequireAuthorization(policy => policy.RequireRole("Admin", "Leader"));

// Registrar visita (Admin ou Leader; Leader só na própria região)
app.MapPost("/admin/visits", async (AppDbContext db, HttpContext ctx, VisitDto dto) =>
{
    var user = await CurrentUserAsync(db, ctx);
    if (user is null) return Results.Unauthorized();

    var colp = await db.Colportors.FindAsync(dto.ColportorId);
    if (colp is null) return Results.NotFound();

    if (user.Role != "Admin")
    {
        if (user.RegionId is null || colp.RegionId != user.RegionId)
            return Results.Forbid();
    }

    // Normaliza a data para UTC e zera hora (meia-noite UTC)
    DateTime d = dto.Date;
    d = d.Kind == DateTimeKind.Unspecified
        ? DateTime.SpecifyKind(d, DateTimeKind.Utc)
        : d.ToUniversalTime();
    d = new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Utc);

    colp.LastVisitDate = d;
    var v = new ColpVisit { ColportorId = dto.ColportorId, Date = d };
    db.Visits.Add(v);
    await db.SaveChangesAsync();

    return Results.Created($"/admin/visits/{v.Id}", new { id = v.Id, colportorId = v.ColportorId, date = v.Date });
}).RequireAuthorization(policy => policy.RequireRole("Admin", "Leader"));

// Países/Regiões (Admin)
app.MapGet("/admin/countries", (AppDbContext db) =>
    db.Countries.Select(c => new { c.Id, c.Name }))
   .RequireAuthorization(policy => policy.RequireRole("Admin"));

app.MapPost("/admin/regions", async (AppDbContext db, RegionCreateDto dto) =>
{
    if (!await db.Countries.AnyAsync(c => c.Id == dto.CountryId))
        return Results.BadRequest("País inexistente.");

    var r = new Region { CountryId = dto.CountryId, Name = dto.Name.Trim() };
    db.Regions.Add(r);
    await db.SaveChangesAsync();
    return Results.Created($"/admin/regions/{r.Id}", new { r.Id, r.Name, r.CountryId });
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// Cadastrar Líder (Admin)
app.MapPost("/admin/leaders", async (AppDbContext db, LeaderRegisterDto dto) =>
{
    if (!await db.Regions.AnyAsync(r => r.Id == dto.RegionId))
        return Results.BadRequest("Região inválida.");

    if (await db.Users.AnyAsync(u => u.Email == dto.Email))
        return Results.Conflict("E-mail já em uso.");

    var user = new ColpUser
    {
        Email = dto.Email.Trim().ToLowerInvariant(),
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        Role = "Leader",
        RegionId = dto.RegionId
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/admin/leaders/{user.Id}", new { user.Id, user.Email, user.RegionId });
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// Lista líderes pendentes de aprovação (Admin)
app.MapGet("/admin/leaders/pending", async (AppDbContext db) =>
{
    var list = await db.Users.Where(u => u.Role == "LeaderPending")
        .Select(u => new { u.Id, u.Email, u.RegionId, Region = u.Region != null ? u.Region.Name : null })
        .OrderBy(u => u.Email).ToListAsync();
    return Results.Ok(list);
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// Aprovar líder pendente (Admin)
app.MapPost("/admin/leaders/{id:int}/approve", async (AppDbContext db, int id) =>
{
    var u = await db.Users.FindAsync(id);
    if (u is null) return Results.NotFound();
    if (u.Role != "LeaderPending") return Results.BadRequest("Usuário não está pendente.");
    if (u.RegionId is null) return Results.BadRequest("Usuário sem região.");
    u.Role = "Leader";
    await db.SaveChangesAsync();
    return Results.Ok(new { u.Id, u.Email, u.RegionId, u.Role });
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// Listar líderes aprovados (Admin)
app.MapGet("/admin/leaders", async (AppDbContext db, int? regionId, string? email) =>
{
    var q = db.Users.Where(u => u.Role == "Leader");
    if (regionId is int rid) q = q.Where(u => u.RegionId == rid);
    if (!string.IsNullOrWhiteSpace(email))
    {
        var term = email.Trim().ToLower();
        q = q.Where(u => u.Email.ToLower().Contains(term));
    }
    var list = await q.Select(u => new { u.Id, u.Email, u.RegionId, Region = u.Region != null ? u.Region.Name : null })
        .OrderBy(u => u.Email).ToListAsync();
    return Results.Ok(list);
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// Atualizar líder (Admin)
app.MapPut("/admin/leaders/{id:int}", async (AppDbContext db, int id, LeaderRegisterDto dto) =>
{
    var u = await db.Users.FindAsync(id);
    if (u is null) return Results.NotFound();
    if (u.Role != "Leader") return Results.BadRequest("Usuário não é líder aprovado.");
    if (!await db.Regions.AnyAsync(r => r.Id == dto.RegionId)) return Results.BadRequest("Região inválida.");
    if (!string.Equals(u.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
    {
        var exists = await db.Users.AnyAsync(x => x.Email == dto.Email && x.Id != id);
        if (exists) return Results.Conflict("E-mail já em uso.");
        u.Email = dto.Email.Trim().ToLowerInvariant();
    }
    if (!string.IsNullOrWhiteSpace(dto.Password))
        u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
    u.RegionId = dto.RegionId;
    await db.SaveChangesAsync();
    return Results.Ok(new { u.Id, u.Email, u.RegionId });
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// Excluir líder (Admin)
app.MapDelete("/admin/leaders/{id:int}", async (AppDbContext db, int id) =>
{
    var u = await db.Users.FindAsync(id);
    if (u is null) return Results.NotFound();
    if (u.Role != "Leader" && u.Role != "LeaderPending") return Results.BadRequest("Usuário não é líder.");
    db.Users.Remove(u);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// ========= CARTEIRA =========
app.MapGet("/wallet/me", async (AppDbContext db, HttpContext ctx) =>
{
    var user = await CurrentUserAsync(db, ctx);
    if (user is null) return Results.Unauthorized();
    if (user.ColportorId is null) return Results.BadRequest();

    var c = await db.Colportors.Include(x => x.Region).Include(x => x.Visits)
                               .SingleAsync(x => x.Id == user.ColportorId);

    var (status, due) = StatusService.ComputeStatus(c.LastVisitDate);
    return Results.Ok(new
    {
        c.Id,
        c.FullName,
        c.CPF,
        Region = c.Region != null ? c.Region.Name : null,
        c.City,
        c.PhotoUrl,
        c.LastVisitDate,
        Status = status,
        DueDate = due
    });
}).RequireAuthorization(policy => policy.RequireRole("Colportor"));

// ========= PAC ENROLLMENTS =========
// Líder cria solicitações de ida ao PAC para colportores da própria região
app.MapPost("/leader/pac/enrollments", async (AppDbContext db, HttpContext ctx, PacEnrollRequest req) =>
{
    var user = await CurrentUserAsync(db, ctx);
    if (user is null) return Results.Unauthorized();
    if (user.Role != "Leader") return Results.Forbid();
    if (user.RegionId is null) return Results.BadRequest("Líder sem região");

    // Normaliza datas para meia-noite UTC inclusivas
    DateTime s = req.StartDate.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(req.StartDate, DateTimeKind.Utc) : req.StartDate.ToUniversalTime();
    DateTime e = req.EndDate.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(req.EndDate, DateTimeKind.Utc) : req.EndDate.ToUniversalTime();
    s = new DateTime(s.Year, s.Month, s.Day, 0, 0, 0, DateTimeKind.Utc);
    e = new DateTime(e.Year, e.Month, e.Day, 0, 0, 0, DateTimeKind.Utc);
    if (e < s) return Results.BadRequest("Período inválido");

    var regionId = user.RegionId.Value;
    var allowed = await db.Colportors.Where(c => req.ColportorIds.Contains(c.Id) && c.RegionId == regionId).Select(c => c.Id).ToListAsync();
    if (!allowed.Any()) return Results.BadRequest("Nenhum colportor válido para sua região");

    foreach (var cid in allowed)
    {
        var enr = new PacEnrollment
        {
            LeaderId = user.Id,
            ColportorId = cid,
            StartDate = s,
            EndDate = e,
            Status = "Pending"
        };
        db.PacEnrollments.Add(enr);
    }
    await db.SaveChangesAsync();
    return Results.Created("/leader/pac/enrollments", new { Count = allowed.Count });
}).RequireAuthorization(policy => policy.RequireRole("Leader"));

// Líder vê apenas suas solicitações
app.MapGet("/leader/pac/enrollments", async (AppDbContext db, HttpContext ctx) =>
{
    var user = await CurrentUserAsync(db, ctx);
    if (user is null) return Results.Unauthorized();
    if (user.Role != "Leader") return Results.Forbid();
    var list = await db.PacEnrollments.Include(x => x.Colportor)
        .Where(x => x.LeaderId == user.Id)
        .OrderByDescending(x => x.CreatedAt)
        .Select(x => new { x.Id, x.Status, x.StartDate, x.EndDate, Colportor = new { x.Colportor.Id, x.Colportor.FullName, x.Colportor.CPF } })
        .ToListAsync();
    return Results.Ok(list);
}).RequireAuthorization(policy => policy.RequireRole("Leader"));

// Admin lista todas (+ filtros) e aprova/reprova
app.MapGet("/admin/pac/enrollments", async (AppDbContext db, int? regionId, string? status, DateTime? from, DateTime? to) =>
{
    var q = db.PacEnrollments.Include(x => x.Colportor).Include(x => x.Leader).AsQueryable();
    if (regionId is int rid) q = q.Where(x => x.Colportor.RegionId == rid);
    if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
    if (from is DateTime f) q = q.Where(x => x.StartDate >= f);
    if (to is DateTime t) q = q.Where(x => x.EndDate <= t);
    var list = await q.OrderBy(x => x.StartDate)
        .Select(x => new { x.Id, x.Status, x.StartDate, x.EndDate, Leader = x.Leader.Email, Colportor = new { x.Colportor.Id, x.Colportor.FullName, x.Colportor.CPF, x.Colportor.RegionId } })
        .ToListAsync();
    return Results.Ok(list);
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

app.MapPost("/admin/pac/enrollments/{id:int}/approve", async (AppDbContext db, int id) =>
{
    var x = await db.PacEnrollments.FindAsync(id);
    if (x is null) return Results.NotFound();
    x.Status = "Approved";
    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

app.MapPost("/admin/pac/enrollments/{id:int}/reject", async (AppDbContext db, int id) =>
{
    var x = await db.PacEnrollments.FindAsync(id);
    if (x is null) return Results.NotFound();
    x.Status = "Rejected";
    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// ========= HEALTH =========
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

Console.WriteLine("🚀 Application starting...");
Console.WriteLine($"🌍 Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"🔗 Listening on: http://+:8080");
Console.WriteLine("✅ Application is ready!");

app.Run();

Console.WriteLine("❌ Application stopped!");
