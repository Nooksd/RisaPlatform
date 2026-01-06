namespace Gateway.Api.Middlewares;

using System.Security.Claims;

public sealed class BillingAccessMiddleware(RequestDelegate next, ILogger<BillingAccessMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<BillingAccessMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        if (!path.Contains("/billing/"))
        {
            await _next(context);
            return;
        }

        var accountType = context.User.FindFirstValue("account_type");

        if (accountType != "TenantAccount")
        {
            _logger.LogWarning("Non-TenantAccount user trying to access billing: {AccountType}", accountType);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "FORBIDDEN", message = "Only tenant owners can access billing" });
            return;
        }

        context.Items["SkipSubscriptionValidation"] = true;

        await _next(context);
    }
}