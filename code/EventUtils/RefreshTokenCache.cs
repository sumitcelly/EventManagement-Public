
using System.Text.RegularExpressions;
using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace EventUtils;

public  class RefreshTokenCache
{

    private readonly IDistributedCache _cache;
    protected readonly double _cacheDurationInMinutes = 60;

    private readonly ILogger<RefreshTokenCache> _logger;

    //private readonly JwtUtils _jwtUtils;
    public RefreshTokenCache(IDistributedCache cache, ILogger<RefreshTokenCache> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache), "Cache cannot be null.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");
        //_jwtUtils = jwtUtils ?? throw new ArgumentNullException(nameof(jwtUtils), "JwtUtils cannot be null.");
    }

    public async Task<bool> ValidateTokenCache(string token, string userId, string tokenId)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

   
        string key = $"refreshToken:{userId}:{tokenId}";
        var cachedToken = await _cache.GetStringAsync(key);
        if (!string.IsNullOrWhiteSpace(cachedToken) && cachedToken.Equals(token))
        {
            return true;
        }
        else
        {
            _logger.LogWarning($"Token validation failed: Token not found in cache for key {key}.");
            return false;
        }

    }
    internal async Task StoreToken(string token, string userId, string tokenId, DateTime expiry)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be null or empty.", nameof(token));

       // (string userId, string tokenId, DateTime expiry) = _jwtUtils.GetUserIdTokenIdAndExpiry(token);
        string key = $"refreshToken:{userId}:{tokenId}";

        var options = new DistributedCacheEntryOptions
        {
             AbsoluteExpiration = expiry
        };

        await _cache.SetStringAsync(key, token, options);
    }

    public async Task InvalidateToken(string token, string userId, string tokenId)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be null or empty.", nameof(token));

        //(string userId, string tokenId, _) = _jwtUtils.GetUserIdTokenIdAndExpiry(token);
        string key = $"refreshToken:{userId}:{tokenId}";

        await _cache.RemoveAsync(key);
    }

}