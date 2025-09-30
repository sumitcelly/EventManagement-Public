
using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventUtils;

public static class CacheHelper
{
    static CacheHelper()
    {
        _logger = null;
    }
 
    public static Microsoft.Extensions.Logging.ILogger _logger { get; set; }
    public static async Task<T?> GetOrSetAsync<T>(this IDistributedCache cache, string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null, Microsoft.Extensions.Logging.ILogger? logger = null) where T : class
    {
        _logger = logger ?? _logger;
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key), "Cache key cannot be null or empty.");
        }

        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory), "Factory function cannot be null.");
        }

        _logger?.LogInformation($"Attempting to get cache for key: {key}");
    
        var cachedValue = await cache.GetStringAsync(key);
        if (cachedValue != null)
        {
            _logger?.LogInformation($"Cache hit for key: {key}");
            return System.Text.Json.JsonSerializer.Deserialize<T>(cachedValue);
        }

        var value = await factory();
        if (value != null)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromMinutes(60)
            };
            await cache.SetStringAsync(key, System.Text.Json.JsonSerializer.Serialize(value), options);
        }

        return value;
    }

    public static async Task<T?> GetOnlyAsync<T>(this IDistributedCache cache, string key, TimeSpan? absoluteExpiration = null, Microsoft.Extensions.Logging.ILogger? logger = null) where T : class
    {
        _logger = logger ?? _logger;
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key), "Cache key cannot be null or empty.");
        }


        _logger?.LogInformation($"Attempting to get cache for key: {key}");
    
        var cachedValue = await cache.GetStringAsync(key);
        if (cachedValue != null)
        {
            _logger?.LogInformation($"Cache hit for key: {key}");
            return System.Text.Json.JsonSerializer.Deserialize<T>(cachedValue);
        }
        else
            return null;
    }

    public static async Task<bool> SetOnlyAsync<T>(this IDistributedCache cache, string key, T data, TimeSpan? absoluteExpiration = null, Microsoft.Extensions.Logging.ILogger? logger = null) where T : class
    {
        _logger = logger ?? _logger;
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key), "Cache key cannot be null or empty.");
        }
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromMinutes(60)
        };
        await cache.SetStringAsync(key, System.Text.Json.JsonSerializer.Serialize(data), options);
        return true;
    }

  
    public static string GetCacheKey<T>(string primaryKey)
    {
        if (string.IsNullOrEmpty(primaryKey))
        {
            throw new ArgumentNullException(nameof(primaryKey), "Primary key cannot be null or empty.");
        }
        string customPrefix = typeof(T).Name;
        if (customPrefix.Contains("List") || customPrefix.Contains("Collection") || customPrefix.Contains("IEnumerable"))
        {
            customPrefix = $"List:{typeof(T).GenericTypeArguments[0].Name}";
        }

        return $"{customPrefix}:{primaryKey}";
    }   


    public static void AddOrUpdateCache<T>(this IDistributedCache cache, T item, string primaryKey, TimeSpan? absoluteExpiration = null)
    {
        var key = GetCacheKey<T>(primaryKey);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromMinutes(60)
        };
        cache.SetString(key, System.Text.Json.JsonSerializer.Serialize(item), options);
    }
    
    public static void RemoveCache<T>(this IDistributedCache cache, string primaryKey)
    {
        var key = GetCacheKey<T>(primaryKey);
        cache.Remove(key);
    }   
}