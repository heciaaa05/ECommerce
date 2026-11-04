using ECommerce.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace ECommerce.API.Health;

// /health/ready: kiểm tra SQL Server và Redis có phản hồi không
public class ReadinessHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _db;
    private readonly IConnectionMultiplexer _redis;

    public ReadinessHealthCheck(ApplicationDbContext db, IConnectionMultiplexer redis)
    {
        _db = db;
        _redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var data = new Dictionary<string, object>();
        var healthy = true;

        try { data["sqlserver"] = await _db.Database.CanConnectAsync(ct) ? "ok" : "fail"; }
        catch (Exception ex) { data["sqlserver"] = "fail: " + ex.Message; }

        try { var ping = await _redis.GetDatabase().PingAsync(); data["redis"] = $"ok ({ping.TotalMilliseconds:F0} ms)"; }
        catch (Exception ex) { data["redis"] = "fail: " + ex.Message; }

        healthy = data.Values.All(v => v.ToString()!.StartsWith("ok"));

        return healthy
            ? HealthCheckResult.Healthy("ready", data)
            : HealthCheckResult.Unhealthy("not ready", data: data);
    }
}
