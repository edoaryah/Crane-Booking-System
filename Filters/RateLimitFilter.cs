using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace AspnetCoreMvcFull.Filters
{
  public class RateLimitFilter : IActionFilter
  {
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitFilter> _logger;
    private readonly int _maxAttempts;
    private readonly TimeSpan _timeWindow;

    public RateLimitFilter(IMemoryCache cache, ILogger<RateLimitFilter> logger, IConfiguration configuration)
    {
      _cache = cache;
      _logger = logger;
      _maxAttempts = configuration.GetValue<int>("Security:RateLimit:MaxAttempts", 5);
      _timeWindow = TimeSpan.FromMinutes(configuration.GetValue<int>("Security:RateLimit:WindowMinutes", 5));
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
      var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
      var cacheKey = $"ratelimit:{ipAddress}";

      // Try to get the previous entry
      if (_cache.TryGetValue(cacheKey, out int attempts))
      {
        if (attempts >= _maxAttempts)
        {
          _logger.LogWarning("Rate limit exceeded for IP {IpAddress}", ipAddress);
          context.Result = new StatusCodeResult(429); // Too Many Requests
          return;
        }

        _cache.Set(cacheKey, attempts + 1, _timeWindow);
      }
      else
      {
        _cache.Set(cacheKey, 1, _timeWindow);
      }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
      // No action needed after execution
    }
  }
}
