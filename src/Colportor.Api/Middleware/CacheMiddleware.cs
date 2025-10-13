using Colportor.Api.Services;
using System.Text.Json;

namespace Colportor.Api.Middleware;

/// <summary>
/// Middleware para cache automático de responses
/// </summary>
public class CacheMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheMiddleware> _logger;

    public CacheMiddleware(RequestDelegate next, ICacheService cacheService, ILogger<CacheMiddleware> logger)
    {
        _next = next;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Apenas cachear GET requests
        if (context.Request.Method != "GET")
        {
            await _next(context);
            return;
        }

        // Gerar chave de cache baseada na URL e query parameters
        var cacheKey = GenerateCacheKey(context.Request);
        
        try
        {
            // Tentar obter do cache
            var cachedResponse = await _cacheService.GetAsync<string>(cacheKey);
            
            if (!string.IsNullOrEmpty(cachedResponse))
            {
                _logger.LogDebug("Cache hit para: {CacheKey}", cacheKey);
                
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(cachedResponse);
                return;
            }

            // Se não estiver no cache, continuar com o request
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            // Verificar se a response foi bem-sucedida
            if (context.Response.StatusCode == 200)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(responseBody).ReadToEndAsync();

                // Cachear a response por 5 minutos (ajustável por endpoint)
                var cacheTime = GetCacheTime(context.Request.Path);
                await _cacheService.SetAsync(cacheKey, responseText, cacheTime);

                _logger.LogDebug("Cache set para: {CacheKey} (TTL: {CacheTime})", cacheKey, cacheTime);

                // Copiar response para o stream original
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            else
            {
                // Para responses de erro, não cachear
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no middleware de cache para: {CacheKey}", cacheKey);
            await _next(context);
        }
    }

    private static string GenerateCacheKey(HttpRequest request)
    {
        var path = request.Path.Value ?? "";
        var query = request.QueryString.Value ?? "";
        
        // Incluir headers importantes para diferenciação de cache
        var userId = request.Headers["Authorization"].FirstOrDefault()?.GetHashCode() ?? 0;
        
        return $"cache:{path}:{query}:{userId}";
    }

    private static TimeSpan GetCacheTime(PathString path)
    {
        // Definir TTL baseado no endpoint
        return path.Value?.ToLower() switch
        {
            "/api/calendar/monthly" => TimeSpan.FromMinutes(10), // Calendário muda menos frequentemente
            "/api/colportor/stats" => TimeSpan.FromMinutes(5),   // Estatísticas
            "/api/region" => TimeSpan.FromHours(1),              // Regiões mudam raramente
            "/api/colportor" => TimeSpan.FromMinutes(2),         // Lista de colportores
            _ => TimeSpan.FromMinutes(1)                         // Default: 1 minuto
        };
    }
}

/// <summary>
/// Extensões para o middleware de cache
/// </summary>
public static class CacheMiddlewareExtensions
{
    public static IApplicationBuilder UseCacheMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CacheMiddleware>();
    }
}
