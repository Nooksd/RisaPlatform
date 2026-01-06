namespace Gateway.Api.Services.Interfaces;

public interface IRateLimitService
{
    Task<(bool allowed, int retryAfter)> IsAllowedAsync(string identifier);
    Task<int> GetRemainingRequestsAsync(string identifier);
    Task ResetAsync(string identifier);
}
