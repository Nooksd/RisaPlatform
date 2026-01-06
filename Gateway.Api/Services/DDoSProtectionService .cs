namespace Gateway.Api.Services;

using Gateway.Api.Services.Interfaces;
using StackExchange.Redis;

public sealed class DDoSProtectionService(IConnectionMultiplexer redis, ILogger<DDoSProtectionService> logger) : IDDoSProtectionService
{
    private readonly IDatabase _database = redis.GetDatabase();
    private readonly ILogger<DDoSProtectionService> _logger = logger;

    private const int SuspiciousRequestThreshold = 50;
    private const int ThreatScoreDecayMinutes = 30;

    public async Task<bool> IsBlacklistedAsync(string ipAddress)
    {
        var key = $"ddos:blacklist:{ipAddress}";
        return await _database.KeyExistsAsync(key);
    }

    public async Task AddToBlacklistAsync(string ipAddress, TimeSpan duration)
    {
        var key = $"ddos:blacklist:{ipAddress}";
        await _database.StringSetAsync(key, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), duration);

        _logger.LogWarning(
            "IP {IpAddress} added to blacklist for {Duration}",
            ipAddress,
            duration);
    }

    public async Task RemoveFromBlacklistAsync(string ipAddress)
    {
        var key = $"ddos:blacklist:{ipAddress}";
        await _database.KeyDeleteAsync(key);

        _logger.LogInformation("IP {IpAddress} removed from blacklist", ipAddress);
    }

    public async Task<bool> DetectSuspiciousPatternAsync(string ipAddress, string path)
    {
        var now = DateTimeOffset.UtcNow;
        var windowKey = $"ddos:requests:{ipAddress}:{now:yyyyMMddHHmmss}";

        var count = await _database.StringIncrementAsync(windowKey);
        await _database.KeyExpireAsync(windowKey, TimeSpan.FromSeconds(15));

        if (count > SuspiciousRequestThreshold)
        {
            return true;
        }

        if (path.Contains("/admin", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/config", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/.env", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/wp-admin", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (path.Contains("'", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("--", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("union", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("select", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public async Task<int> IncrementThreatScoreAsync(string ipAddress)
    {
        var key = $"ddos:threatscore:{ipAddress}";
        var score = await _database.StringIncrementAsync(key, 10);
        await _database.KeyExpireAsync(key, TimeSpan.FromMinutes(ThreatScoreDecayMinutes));

        return (int)score;
    }

    public async Task ResetThreatScoreAsync(string ipAddress)
    {
        var key = $"ddos:threatscore:{ipAddress}";
        await _database.KeyDeleteAsync(key);
    }
}