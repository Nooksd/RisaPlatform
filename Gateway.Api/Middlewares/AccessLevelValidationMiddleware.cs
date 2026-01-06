namespace Gateway.Api.Middlewares;

using System.Security.Claims;
using System.Text.Json;

public sealed class AccessLevelValidationMiddleware(RequestDelegate next, ILogger<AccessLevelValidationMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<AccessLevelValidationMiddleware> _logger = logger;

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

        var accountType = context.User.FindFirstValue("account_type");

        if (accountType == "TenantAccount")
        {
            await _next(context);
            return;
        }

        if (accountType == "TenantUser")
        {
            if (!context.Items.TryGetValue("RequestedModule", out var moduleObj) || moduleObj is not string module)
            {
                await _next(context);
                return;
            }

            var moduleAccessClaim = context.User.FindFirstValue("module_accesses");

            if (string.IsNullOrEmpty(moduleAccessClaim))
            {
                _logger.LogWarning("TenantUser without module_accesses claim trying to access {Module}", module);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "NO_ACCESS", message = "You don't have access to this module" });
                return;
            }

            var moduleAccesses = JsonSerializer.Deserialize<Dictionary<string, int>>(moduleAccessClaim);

            if (moduleAccesses == null || !moduleAccesses.TryGetValue(module, out var accessLevel) || accessLevel < 1)
            {
                _logger.LogWarning("TenantUser trying to access {Module} without sufficient permissions", module);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "INSUFFICIENT_PERMISSIONS", message = $"You don't have access to {module}" });
                return;
            }

            await _next(context);
            return;
        }

        _logger.LogWarning("PublicUser trying to access protected module");
        context.Response.StatusCode = 403;
        await context.Response.WriteAsJsonAsync(new { error = "FORBIDDEN", message = "Access denied" });
    }
}