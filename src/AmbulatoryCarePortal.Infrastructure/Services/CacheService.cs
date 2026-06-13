using System.Text.Json;
using AmbulatoryCarePortal.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AmbulatoryCarePortal.Infrastructure.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(10);

    public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        var value = await _cache.GetStringAsync(key, ct);
        if (value == null)
        {
            _logger.LogDebug("Cache miss: {Key}", key);
            return null;
        }

        _logger.LogDebug("Cache hit: {Key}", key);
        return JsonSerializer.Deserialize<T>(value);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? DefaultTtl
        };

        var json = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, json, options, ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await _cache.RemoveAsync(key, ct);
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached != null)
            return cached;

        var value = await factory();
        if (value != null)
            await SetAsync(key, value, ttl, ct);

        return value;
    }
}
