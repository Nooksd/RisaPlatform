namespace Gateway.Api.Middlewares;

using System.Security.Claims;

public sealed class TenantValidationMiddleware(RequestDelegate next, ILogger<TenantValidationMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<TenantValidationMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        if (path.StartsWith("/api/auth/tenant") ||
            path.StartsWith("/api/auth/refresh") ||
            path.StartsWith("/api/auth/logout") ||
            path.StartsWith("/api/auth/me"))
        {
            await _next(context);
            return;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length < 3 || segments[0] != "api")
        {
            await _next(context);
            return;
        }

        if (!Guid.TryParse(segments[1], out var routeTenantId))
        {
            await _next(context);
            return;
        }

        var tenantIdsClaim = context.User.FindFirstValue("tenant_ids");

        if (string.IsNullOrEmpty(tenantIdsClaim))
        {
            _logger.LogWarning("User without tenant_ids trying to access {Path}", path);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "FORBIDDEN", message = "Access denied" });
            return;
        }

        var userTenantIds = tenantIdsClaim
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s.Trim(), out var id) ? id : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();

        if (!userTenantIds.Contains(routeTenantId))
        {
            _logger.LogWarning("User trying to access tenant {TenantId} without permission", routeTenantId);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "FORBIDDEN", message = "You don't have access to this tenant" });
            return;
        }

        context.Items["TenantId"] = routeTenantId;

        await _next(context);
    }
}