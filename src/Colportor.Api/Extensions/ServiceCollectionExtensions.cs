using Microsoft.EntityFrameworkCore;
using Colportor.Api.Data;
using Colportor.Api.Repositories;
using Colportor.Api.Services;
using Colportor.Api.Mapping;
using AutoMapper;
using FluentValidation;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using StackExchange.Redis;
using AspNetCoreRateLimit;

namespace Colportor.Api.Extensions;

/// <summary>
/// Extensões para configuração de serviços
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configura serviços de banco de dados
    /// </summary>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configurar Entity Framework
        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = GetConnectionString(configuration);
            options.UseNpgsql(connectionString);
            options.EnableSensitiveDataLogging(false); // Apenas para desenvolvimento
        });

        // Configurar repositórios
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IColportorRepository, ColportorRepository>();
        services.AddScoped<IRegionRepository, RegionRepository>();

        return services;
    }

    /// <summary>
    /// Configura serviços de aplicação
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Configurar cache distribuído (usando Memory Cache por enquanto)
        services.AddDistributedMemoryCache();
        
        // Serviços de domínio
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IColportorService, ColportorService>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<IRegionService, RegionService>();

        // Serviços de infraestrutura
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IPaginationService, PaginationService>();

        // AutoMapper
        services.AddAutoMapper(typeof(AutoMapperProfile));

        // FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }

    /// <summary>
    /// Configura logging estruturado
    /// </summary>
    public static IServiceCollection AddStructuredLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog((serviceProvider, loggerConfig) =>
        {
            loggerConfig
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(serviceProvider)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/colportor-.log", 
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
        });

        return services;
    }

    /// <summary>
    /// Configura cache Redis
    /// </summary>
    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "ColportorAPI";
            });

            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                return ConnectionMultiplexer.Connect(redisConnectionString);
            });
        }
        else
        {
            // Fallback para cache em memória se Redis não estiver disponível
            services.AddMemoryCache();
        }

        return services;
    }

    /// <summary>
    /// Configura rate limiting
    /// </summary>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.Configure<IpRateLimitOptions>(options =>
        {
            options.EnableEndpointRateLimiting = true;
            options.StackBlockedRequests = false;
            options.HttpStatusCode = 429;
            options.RealIpHeader = "X-Real-IP";
            options.ClientIdHeader = "X-ClientId";
            
            options.GeneralRules = new List<RateLimitRule>
            {
                new RateLimitRule
                {
                    Endpoint = "*",
                    Period = "1m",
                    Limit = 100
                },
                new RateLimitRule
                {
                    Endpoint = "POST:/api/auth/login",
                    Period = "5m",
                    Limit = 5
                },
                new RateLimitRule
                {
                    Endpoint = "POST:/api/auth/register",
                    Period = "1h",
                    Limit = 3
                }
            };
        });

        services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

        return services;
    }

    /// <summary>
    /// Configura CORS
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigins", policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:3000",
                        "http://localhost:8080",
                        "https://localhost:5001",
                        "https://colportor-management-system-production.up.railway.app"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });

            options.AddPolicy("AllowAll", policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }

    /// <summary>
    /// Configura JWT Authentication
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration["JWT:Key"];
        var jwtIssuer = configuration["JWT:Issuer"] ?? "ColportorAPI";
        var jwtAudience = configuration["JWT:Audience"] ?? "ColportorClient";

        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("JWT:Key não configurado");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                    logger.LogWarning("Falha na autenticação JWT: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                    logger.LogDebug("Token JWT validado com sucesso");
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();

        return services;
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        
        if (environment == "Production")
        {
            var databaseUrl = configuration["DATABASE_URL"];
            
            if (!string.IsNullOrEmpty(databaseUrl))
            {
                if (databaseUrl.StartsWith("postgresql://"))
                {
                    var uri = new Uri(databaseUrl);
                    var host = uri.Host;
                    var port = uri.Port;
                    var database = uri.AbsolutePath.TrimStart('/');
                    var username = uri.UserInfo.Split(':')[0];
                    var password = uri.UserInfo.Split(':')[1];
                    
                    return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
                }
                else
                {
                    return databaseUrl;
                }
            }
        }

        return configuration.GetConnectionString("Default") 
               ?? configuration.GetConnectionString("DefaultConnection")
               ?? "Host=localhost;Database=colportor_db;Username=postgres;Password=postgres";
    }
}
