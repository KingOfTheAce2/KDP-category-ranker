using KDP_CATEGORY_RANKERScraping.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KDP_CATEGORY_RANKERScraping.Services;

public class ScrapingCacheService : IScrapingCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<ScrapingCacheService> _logger;
    private readonly TimeSpan _defaultExpiration;

    public ScrapingCacheService(
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<ScrapingCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
        _defaultExpiration = TimeSpan.FromHours(
            configuration.GetValue<double>("Scraping:CacheExpirationHours", 24));
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cached as T;
            }
            
            _logger.LogDebug("Cache miss for key: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache with key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
                Priority = CacheItemPriority.Normal,
                SlidingExpiration = TimeSpan.FromHours(1)
            };

            _cache.Set(key, value, options);
            _logger.LogDebug("Cached item with key: {Key}, expiration: {Expiration}", 
                key, expiration ?? _defaultExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache with key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            _cache.Remove(key);
            _logger.LogDebug("Removed cache item with key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache item with key: {Key}", key);
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            if (_cache is MemoryCache memoryCache)
            {
                var field = typeof(MemoryCache).GetField("_coherentState", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (field?.GetValue(memoryCache) is IDictionary dict)
                {
                    dict.Clear();
                }
            }
            
            _logger.LogInformation("Cache cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
        }
    }

    public string GenerateKey(string operation, params object[] parameters)
    {
        var keyParts = new List<string> { operation };
        
        foreach (var param in parameters)
        {
            if (param != null)
            {
                keyParts.Add(param.ToString() ?? string.Empty);
            }
        }
        
        var key = string.Join(":", keyParts);
        return key.Replace(" ", "_").ToLowerInvariant();
    }
}