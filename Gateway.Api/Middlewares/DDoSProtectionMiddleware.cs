using Gateway.Api.Services.Interfaces;

namespace Gateway.Api.Middlewares;

public sealed class DDoSProtectionMiddleware(
    RequestDelegate next,
    IDDoSProtectionService ddosService,
    ILogger<DDoSProtectionMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly IDDoSProtectionService _ddosService = ddosService;
    private readonly ILogger<DDoSProtectionMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = GetClientIpAddress(context);

        if (await _ddosService.IsBlacklistedAsync(ipAddress))
        {
            _logger.LogWarning("Blocked request from blacklisted IP: {IpAddress}", ipAddress);

            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "FORBIDDEN",
                message = "Access denied"
            });
            return;
        }

        var isSuspicious = await _ddosService.DetectSuspiciousPatternAsync(ipAddress, context.Request.Path);

        if (isSuspicious)
        {
            _logger.LogWarning(
                "Suspicious activity detected from IP {IpAddress} on path {Path}",
                ipAddress,
                context.Request.Path);

            var threatScore = await _ddosService.IncrementThreatScoreAsync(ipAddress);

            if (threatScore > 100)
            {
                await _ddosService.AddToBlacklistAsync(ipAddress, TimeSpan.FromHours(1));

                _logger.LogError(
                    "IP {IpAddress} added to blacklist due to high threat score: {Score}",
                    ipAddress,
                    threatScore);

                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "FORBIDDEN",
                    message = "Access denied due to suspicious activity"
                });
                return;
            }
        }

        await _next(context);
    }

    private static string GetClientIpAddress(HttpContext context)
    {
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