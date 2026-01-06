namespace Gateway.Api.Middlewares;

public sealed class TokenValidationMiddleware(RequestDelegate next, ILogger<TokenValidationMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<TokenValidationMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        if (path.StartsWith("/api/auth/tenant/register") ||
            path.StartsWith("/api/auth/tenant/login") ||
            path.Contains("/api/auth/") && path.Contains("/public/register") ||
            path.Contains("/api/auth/") && path.Contains("/public/login") ||
            path.StartsWith("/api/auth/refresh"))
        {
            await _next(context);
            return;
        }

        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("Unauthorized access attempt to {Path}", path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "UNAUTHORIZED", message = "Token is required" });
            return;
        }

        await _next(context);
    }
}