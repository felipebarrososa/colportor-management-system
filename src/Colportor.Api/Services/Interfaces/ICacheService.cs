using System.Text.Json;

namespace Colportor.Api.Services;

/// <summary>
/// Interface para serviço de cache
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Obtém item do cache
    /// </summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Define item no cache
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);

    /// <summary>
    /// Remove item do cache
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Remove múltiplos itens do cache
    /// </summary>
    Task RemoveByPatternAsync(string pattern);

    /// <summary>
    /// Verifica se chave existe no cache
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Obtém ou define item no cache
    /// </summary>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null);

    /// <summary>
    /// Define múltiplos itens no cache
    /// </summary>
    Task SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiry = null);

    /// <summary>
    /// Obtém múltiplos itens do cache
    /// </summary>
    Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys);
}
