using Billing.Domain.Entities;

namespace Billing.Domain.Interfaces.Repositories;

public interface ISubscriptionRepository : IRepository<Subscription>
{
    Task<Subscription?> GetActiveByTenantBillingIdAsync(Guid tenantBillingId, CancellationToken ct = default);
    Task<Subscription?> GetWithModulesAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Subscription>> GetByTenantBillingIdAsync(Guid tenantBillingId, CancellationToken ct = default);
    Task<IEnumerable<Subscription>> GetExpiringInDaysAsync(int days, CancellationToken ct = default);
}
