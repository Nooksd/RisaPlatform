namespace Gateway.Api.Middlewares;

using Gateway.Api.Services.Interfaces;

public sealed class SubscriptionValidationMiddleware(
    RequestDelegate next,
    ISubscriptionCache subscriptionCache,
    ILogger<SubscriptionValidationMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ISubscriptionCache _subscriptionCache = subscriptionCache;
    private readonly ILogger<SubscriptionValidationMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Items.ContainsKey("SkipSubscriptionValidation"))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value?.ToLower() ?? "";

        if (path.StartsWith("/api/auth/"))
        {
            await _next(context);
            return;
        }

        if (!context.Items.TryGetValue("TenantId", out var tenantIdObj) || tenantIdObj is not Guid tenantId)
        {
            await _next(context);
            return;
        }

        var subscription = await _subscriptionCache.GetAsync(tenantId);

        if (subscription == null)
        {
            _logger.LogWarning("Tenant {TenantId} has no active subscription", tenantId);
            context.Response.StatusCode = 402;
            await context.Response.WriteAsJsonAsync(new { error = "NO_SUBSCRIPTION", message = "No active subscription found" });
            return;
        }

        var isExpired = subscription.ExpiresAt < DateTime.UtcNow;

        if (isExpired)
        {
            if (path.Contains("/tenantuser/login") || path.Contains("/public/"))
            {
                _logger.LogWarning("Tenant {TenantId} subscription expired, blocking login", tenantId);
                context.Response.StatusCode = 402;
                await context.Response.WriteAsJsonAsync(new { error = "SUBSCRIPTION_EXPIRED", message = "Subscription expired. Please renew to continue." });
                return;
            }

            if (path.StartsWith("/api/auth/"))
            {
                context.Request.Headers["X-Tenant-Subscription-Status"] = "expired";
                context.Request.Headers["X-Tenant-User-Limit"] = "0";
                context.Request.Headers["X-Tenant-Expires-At"] = subscription.ExpiresAt.ToString("O");
                await _next(context);
                return;
            }

            context.Response.StatusCode = 402;
            await context.Response.WriteAsJsonAsync(new { error = "SUBSCRIPTION_EXPIRED", message = "Subscription expired" });
            return;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length < 3)
        {
            await _next(context);
            return;
        }

        var module = segments[2].ToUpper();

        if (!subscription.Modules.Contains(module, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Tenant {TenantId} trying to access module {Module} not in plan", tenantId, module);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "MODULE_NOT_IN_PLAN", message = $"Module {module} is not in your plan" });
            return;
        }

        context.Request.Headers["X-Tenant-Subscription-Status"] = "active";
        context.Request.Headers["X-Tenant-User-Limit"] = subscription.UserLimit.ToString();
        context.Request.Headers["X-Tenant-Expires-At"] = subscription.ExpiresAt.ToString("O");

        context.Items["Subscription"] = subscription;
        context.Items["RequestedModule"] = module;

        await _next(context);
    }
}