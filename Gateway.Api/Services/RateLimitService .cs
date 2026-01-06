namespace Gateway.Api.Services;

using Gateway.Api.Services.Interfaces;
using StackExchange.Redis;

public sealed class RateLimitService(IConnectionMultiplexer redis, ILogger<RateLimitService> logger) : IRateLimitService
{
    private readonly IDatabase _database = redis.GetDatabase();
    private readonly ILogger<RateLimitService> _logger = logger;

    private const int MaxRequestsPerMinute = 100;
    private const int MaxRequestsPer5Minutes = 300;
    private const int MaxRequestsPerHour = 1000;

    private const int BaseBackoffSeconds = 60;
    private const int MaxBackoffSeconds = 7200;

    public async Task<(bool allowed, int retryAfter)> IsAllowedAsync(string identifier)
    {
        var now = DateTimeOffset.UtcNow;

        var lockoutKey = $"ratelimit:lockout:{identifier}";
        var lockoutValue = await _database.StringGetAsync(lockoutKey);

        if (!lockoutValue.IsNullOrEmpty)
        {
            var lockoutUntil = DateTimeOffset.FromUnixTimeSeconds(long.Parse(lockoutValue!));
            var retryAfter = (int)(lockoutUntil - now).TotalSeconds;

            if (retryAfter > 0)
            {
                return (false, retryAfter);
            }

            await _database.KeyDeleteAsync(lockoutKey);
        }

        var minuteKey = $"ratelimit:1m:{identifier}:{now:yyyyMMddHHmm}";
        var fiveMinKey = $"ratelimit:5m:{identifier}:{now:yyyyMMddHHmm}";
        var hourKey = $"ratelimit:1h:{identifier}:{now:yyyyMMddHH}";
        var violationsKey = $"ratelimit:violations:{identifier}";

        var transaction = _database.CreateTransaction();

        var minuteTask = transaction.StringIncrementAsync(minuteKey);
        var fiveMinTask = transaction.StringIncrementAsync(fiveMinKey);
        var hourTask = transaction.StringIncrementAsync(hourKey);

        transaction.KeyExpireAsync(minuteKey, TimeSpan.FromMinutes(2));
        transaction.KeyExpireAsync(fiveMinKey, TimeSpan.FromMinutes(10));
        transaction.KeyExpireAsync(hourKey, TimeSpan.FromHours(2));

        await transaction.ExecuteAsync();

        var minuteCount = await minuteTask;
        var fiveMinCount = await fiveMinTask;
        var hourCount = await hourTask;

        if (minuteCount > MaxRequestsPerMinute ||
            fiveMinCount > MaxRequestsPer5Minutes ||
            hourCount > MaxRequestsPerHour)
        {
            var violations = await _database.StringIncrementAsync(violationsKey);
            await _database.KeyExpireAsync(violationsKey, TimeSpan.FromHours(24));

            var backoffSeconds = CalculateExponentialBackoff((int)violations);

            var lockoutUntil = now.AddSeconds(backoffSeconds).ToUnixTimeSeconds();
            await _database.StringSetAsync(lockoutKey, lockoutUntil, TimeSpan.FromSeconds(backoffSeconds));

            _logger.LogWarning(
                "Rate limit violation #{Violations} for {Identifier}. Locked out for {Seconds}s. " +
                "Counts: 1m={Minute}, 5m={FiveMin}, 1h={Hour}",
                violations,
                identifier,
                backoffSeconds,
                minuteCount,
                fiveMinCount,
                hourCount);

            return (false, backoffSeconds);
        }

        return (true, 0);
    }

    public async Task<int> GetRemainingRequestsAsync(string identifier)
    {
        var now = DateTimeOffset.UtcNow;
        var minuteKey = $"ratelimit:1m:{identifier}:{now:yyyyMMddHHmm}";

        var count = await _database.StringGetAsync(minuteKey);
        if (count.IsNullOrEmpty)
        {
            return MaxRequestsPerMinute;
        }

        var used = int.Parse(count!);
        return Math.Max(0, MaxRequestsPerMinute - used);
    }

    public async Task ResetAsync(string identifier)
    {
        var keys = new[]
        {
            $"ratelimit:lockout:{identifier}",
            $"ratelimit:violations:{identifier}"
        };

        await _database.KeyDeleteAsync(keys.Select(k => (RedisKey)k).ToArray());

        _logger.LogInformation("Rate limit reset for {Identifier}", identifier);
    }

    private static int CalculateExponentialBackoff(int violations)
    {
        if (violations <= 0) return BaseBackoffSeconds;

        var backoff = BaseBackoffSeconds * Math.Pow(2, violations - 1);
        return (int)Math.Min(backoff, MaxBackoffSeconds);
    }
}