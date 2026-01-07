using Gateway.Api.Services.Interfaces;

namespace Gateway.Api.Middlewares;

public sealed class DDoSProtectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDDoSProtectionService _ddosService;
    private readonly ILogger<DDoSProtectionMiddleware> _logger;

    public DDoSProtectionMiddleware(
        RequestDelegate next,
        IDDoSProtectionService ddosService,
        ILogger<DDoSProtectionMiddleware> logger)
    {
        _next = next;
        _ddosService = ddosService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = GetClientIpAddress(context);

        // Verificar se IP está na blacklist
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

        // Detectar padrões suspeitos
        var isSuspicious = await _ddosService.DetectSuspiciousPatternAsync(ipAddress, context.Request.Path);

        if (isSuspicious)
        {
            _logger.LogWarning(
                "Suspicious activity detected from IP {IpAddress} on path {Path}",
                ipAddress,
                context.Request.Path);

            // Adicionar ao sistema de pontuação de ameaças
            var threatScore = await _ddosService.IncrementThreatScoreAsync(ipAddress);

            // Se score muito alto, adicionar à blacklist temporária
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