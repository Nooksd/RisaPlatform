namespace Gateway.Api.Services;

using Gateway.Api.Models;
using Gateway.Api.Services.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

public sealed class SubscriptionCache(IConnectionMultiplexer redis) : ISubscriptionCache
{
    private readonly IDatabase _database = redis.GetDatabase();

    public async Task<TenantSubscription?> GetAsync(Guid tenantId, CancellationToken ct = default)
    {
        var key = $"tenant:{tenantId}";
        var value = await _database.StringGetAsync(key);

        if (value.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<TenantSubscription>(value.ToString());
    }

    public async Task SetAsync(Guid tenantId, TenantSubscription subscription, CancellationToken ct = default)
    {
        var key = $"tenant:{tenantId}";
        var value = JsonSerializer.Serialize(subscription);

        var expiry = subscription.ExpiresAt - DateTime.UtcNow;
        if (expiry.TotalSeconds > 0)
        {
            await _database.StringSetAsync(key, value, expiry);
        }
        else
        {
            await _database.StringSetAsync(key, value);
        }
    }

    public async Task DeleteAsync(Guid tenantId, CancellationToken ct = default)
    {
        var key = $"tenant:{tenantId}";
        await _database.KeyDeleteAsync(key);
    }
}