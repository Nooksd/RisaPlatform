namespace Gateway.Api.Middlewares;

public sealed class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IRateLimitService rateLimitService,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Obter IP real (considerando proxy/ingress)
        var ipAddress = GetClientIpAddress(context);

        // Verificar rate limit
        var (allowed, retryAfter) = await _rateLimitService.IsAllowedAsync(ipAddress);

        if (!allowed)
        {
            _logger.LogWarning(
                "Rate limit exceeded for IP {IpAddress}. Retry after {RetryAfter} seconds",
                ipAddress,
                retryAfter);

            context.Response.StatusCode = 429; // Too Many Requests
            context.Response.Headers["Retry-After"] = retryAfter.ToString();
            context.Response.Headers["X-RateLimit-Limit"] = "100";
            context.Response.Headers["X-RateLimit-Remaining"] = "0";
            context.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddSeconds(retryAfter).ToUnixTimeSeconds().ToString();

            await context.Response.WriteAsJsonAsync(new
            {
                error = "RATE_LIMIT_EXCEEDED",
                message = $"Too many requests. Please retry after {retryAfter} seconds.",
                retryAfter
            });

            return;
        }

        // Adicionar headers informativos
        var remaining = await _rateLimitService.GetRemainingRequestsAsync(ipAddress);
        context.Response.Headers["X-RateLimit-Limit"] = "100";
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();

        await _next(context);
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        //順序: X-Forwarded-For (Ingress/Proxy) → X-Real-IP → RemoteIpAddress
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            var ips = xForwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}