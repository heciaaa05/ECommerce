using System.Text.Json;
using ECommerce.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ECommerce.Infrastructure.Caching;

/// <summary>
/// Cache qua Redis. Redis lỗi/chết thì coi như cache miss (app vẫn chạy bằng DB), chỉ ghi log cảnh báo.
/// Xoá theo prefix dùng SCAN trên Redis nên đúng cả khi chạy nhiều instance API.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IDistributedCache cache, IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _redis = redis;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var data = await _cache.GetStringAsync(key);
            return data is null ? default : JsonSerializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET lỗi, bỏ qua cache cho key {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiry)
    {
        try
        {
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry };
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET lỗi cho key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis REMOVE lỗi cho key {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix)
    {
        try
        {
            var db = _redis.GetDatabase();
            foreach (var endpoint in _redis.GetEndPoints())
            {
                var server = _redis.GetServer(endpoint);
                if (!server.IsConnected || server.IsReplica) continue;

                var keys = server.Keys(pattern: prefix + "*").ToArray();
                if (keys.Length > 0)
                    await db.KeyDeleteAsync(keys);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis xoá theo prefix {Prefix} lỗi", prefix);
        }
    }
}
