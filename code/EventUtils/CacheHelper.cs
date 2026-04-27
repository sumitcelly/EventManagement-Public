
using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventUtils;

using System.Text.Json;
using System.Text.Json.Serialization;

public class LocalNoZDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetDateTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // This forces the format to ignore the timezone/Z
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss"));
    }
}

public static class CacheHelper
{
    public static JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
           
    static CacheHelper()
    {
        _logger = null;
         serializerOptions.Converters.Add(new LocalNoZDateTimeConverter());
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
          
            await cache.SetStringAsync(key, System.Text.Json.JsonSerializer.Serialize(value, serializerOptions), options);
        }

        return value;
    }


    /// <summary>
    /// Send key obtained from GetCacheKey. So a string Event:24 not just 24 which is primary key
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="cache"></param>
    /// <param name="key"></param>
    /// <param name="absoluteExpiration"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
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
        await cache.SetStringAsync(key, System.Text.Json.JsonSerializer.Serialize(data,serializerOptions), options);
        return true;
    }

  
  
    /// <summary>
    /// Send only the primary key like event  id or customerid
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="primaryKey"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
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


    /// <summary>
    /// Send primary key ONLY. Like 24 for event id. Do not send key from GetCacheKey
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="cache"></param>
    /// <param name="item"></param>
    /// <param name="primaryKey"></param>
    /// <param name="absoluteExpiration"></param>
    public static void AddOrUpdateCache<T>(this IDistributedCache cache, T item, string primaryKey, TimeSpan? absoluteExpiration = null)
    {
        var key = GetCacheKey<T>(primaryKey);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromMinutes(60)
        };
        cache.SetString(key, System.Text.Json.JsonSerializer.Serialize(item,serializerOptions), options);
    }
    
    public static void RemoveCache<T>(this IDistributedCache cache, string primaryKey)
    {
        var key = GetCacheKey<T>(primaryKey);
        cache.Remove(key);
    }   
}