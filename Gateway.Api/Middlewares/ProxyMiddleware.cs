namespace Gateway.Api.Middlewares;

using Gateway.Api.Services.Interfaces;

public sealed class ProxyMiddleware(RequestDelegate next, IProxyService proxyService, ILogger<ProxyMiddleware> logger)
{
    private readonly IProxyService _proxyService = proxyService;
    private readonly ILogger<ProxyMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        if (path.StartsWith("/api/auth/"))
        {
            await _proxyService.ProxyRequestAsync(context, "AuthService");
            return;
        }

        if (path.Contains("/billing/"))
        {
            await _proxyService.ProxyRequestAsync(context, "BillingService");
            return;
        }

        if (path.Contains("/crm/"))
        {
            await _proxyService.ProxyRequestAsync(context, "CrmService");
            return;
        }

        _logger.LogWarning("No route found for path: {Path}", path);
        context.Response.StatusCode = 404;
        await context.Response.WriteAsJsonAsync(new { error = "NOT_FOUND", message = "Route not found" });
    }
}