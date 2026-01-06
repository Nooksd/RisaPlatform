namespace Gateway.Api.Services.Interfaces;

using Gateway.Api.Models;

public interface ISubscriptionCache
{
    Task<TenantSubscription?> GetAsync(Guid tenantId, CancellationToken ct = default);
    Task SetAsync(Guid tenantId, TenantSubscription subscription, CancellationToken ct = default);
    Task DeleteAsync(Guid tenantId, CancellationToken ct = default);
}