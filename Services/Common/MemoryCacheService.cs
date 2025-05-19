using Microsoft.Extensions.Caching.Memory;

namespace AspnetCoreMvcFull.Services.Common
{
  public class MemoryCacheService : ICacheService
  {
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;

    public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
    {
      _memoryCache = memoryCache;
      _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key)
    {
      try
      {
        if (_memoryCache.TryGetValue(key, out T? cachedValue))
        {
          return Task.FromResult(cachedValue);
        }
        return Task.FromResult<T?>(default);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving value from cache for key {Key}", key);
        return Task.FromResult<T?>(default);
      }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
      try
      {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(expiration)
            .SetPriority(CacheItemPriority.Normal);

        _memoryCache.Set(key, value, cacheEntryOptions);
        return Task.CompletedTask;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error setting cache value for key {Key}", key);
        return Task.CompletedTask;
      }
    }

    public Task RemoveAsync(string key)
    {
      try
      {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error removing cache value for key {Key}", key);
        return Task.CompletedTask;
      }
    }

    public Task<bool> ExistsAsync(string key)
    {
      return Task.FromResult(_memoryCache.TryGetValue(key, out _));
    }
  }
}
