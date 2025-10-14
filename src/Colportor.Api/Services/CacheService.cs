using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using System.Text.Json;

namespace Colportor.Api.Services;

/// <summary>
/// Implementação do serviço de cache com Redis e fallback para memória
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<CacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CacheService(
        IDistributedCache distributedCache,
        IMemoryCache memoryCache,
        ILogger<CacheService> logger)
    {
        _distributedCache = distributedCache;
        _memoryCache = memoryCache;
        _redis = null; // Redis não está configurado ainda
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            // Tentar Redis primeiro
            if (_redis != null && _redis.IsConnected)
            {
                var value = await _distributedCache.GetStringAsync(key);
                if (!string.IsNullOrEmpty(value))
                {
                    return JsonSerializer.Deserialize<T>(value, _jsonOptions);
                }
            }

            // Fallback para memória
            if (_memoryCache.TryGetValue(key, out T? cachedValue))
            {
                _logger.LogDebug("Cache hit (memory) para chave: {Key}", key);
                return cachedValue;
            }

            _logger.LogDebug("Cache miss para chave: {Key}", key);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter cache para chave: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions();

            if (expiry.HasValue)
            {
                options.SetAbsoluteExpiration(expiry.Value);
            }
            else
            {
                options.SetAbsoluteExpiration(TimeSpan.FromHours(1)); // Default 1 hora
            }

            // Tentar Redis primeiro
            if (_redis != null && _redis.IsConnected)
            {
                await _distributedCache.SetStringAsync(key, serializedValue, options);
                _logger.LogDebug("Cache set (Redis) para chave: {Key}", key);
            }
            else
            {
                // Fallback para memória
                var memoryOptions = new MemoryCacheEntryOptions();
                if (expiry.HasValue)
                {
                    memoryOptions.SetAbsoluteExpiration(expiry.Value);
                }
                else
                {
                    memoryOptions.SetAbsoluteExpiration(TimeSpan.FromHours(1));
                }

                _memoryCache.Set(key, value, memoryOptions);
                _logger.LogDebug("Cache set (memory) para chave: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao definir cache para chave: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            // Tentar Redis primeiro
            if (_redis != null && _redis.IsConnected)
            {
                await _distributedCache.RemoveAsync(key);
                _logger.LogDebug("Cache removido (Redis) para chave: {Key}", key);
            }

            // Sempre remover da memória também
            _memoryCache.Remove(key);
            _logger.LogDebug("Cache removido (memory) para chave: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover cache para chave: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            if (_redis != null && _redis.IsConnected)
            {
                var database = _redis.GetDatabase();
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                
                var keys = server.Keys(pattern: pattern).ToArray();
                if (keys.Length > 0)
                {
                    await database.KeyDeleteAsync(keys);
                    _logger.LogDebug("Cache removido por padrão (Redis): {Pattern} ({Count} chaves)", pattern, keys.Length);
                }
            }

            // Para memória, não temos suporte nativo a padrões
            // Em produção, você poderia implementar um registry de chaves
            _logger.LogWarning("Remoção por padrão não suportada para cache em memória: {Pattern}", pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover cache por padrão: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            if (_redis != null && _redis.IsConnected)
            {
                var database = _redis.GetDatabase();
                return await database.KeyExistsAsync(key);
            }

            return _memoryCache.TryGetValue(key, out _);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar existência do cache para chave: {Key}", key);
            return false;
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
    {
        try
        {
            var cachedValue = await GetAsync<T>(key);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            var value = await factory();
            await SetAsync(key, value, expiry);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no GetOrSet para chave: {Key}", key);
            // Retornar valor da factory mesmo se cache falhar
            return await factory();
        }
    }

    public async Task SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiry = null)
    {
        try
        {
            var tasks = items.Select(kvp => SetAsync(kvp.Key, kvp.Value, expiry));
            await Task.WhenAll(tasks);
            
            _logger.LogDebug("Cache set (múltiplos) para {Count} chaves", items.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao definir múltiplos itens no cache");
        }
    }

    public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys)
    {
        var result = new Dictionary<string, T?>();
        
        try
        {
            var tasks = keys.Select(async key =>
            {
                var value = await GetAsync<T>(key);
                return new KeyValuePair<string, T?>(key, value);
            });

            var results = await Task.WhenAll(tasks);
            
            foreach (var kvp in results)
            {
                result[kvp.Key] = kvp.Value;
            }

            _logger.LogDebug("Cache get (múltiplos) para {Count} chaves", keys.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter múltiplos itens do cache");
        }

        return result;
    }
}
