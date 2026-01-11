namespace Gateway.Api.Middlewares.Policy;

public static class GatewayRoutePolicy
{
    private static readonly string[] PublicPrefixes =
    [
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/me",
        "/api/auth/refresh",
        "/health",
        "/swagger"
    ];

    public static bool IsPublic(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        return PublicPrefixes.Any(p => path.StartsWith(p));
    }
}
