using Billing.Domain.Entities;
using Billing.Domain.Enums;

namespace Billing.Domain.Interfaces.Repositories;

public interface ITenantBillingRepository : IRepository<TenantBilling>
{
    Task<TenantBilling?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<TenantBilling?> GetByTenantAccountIdAsync(Guid tenantAccountId, CancellationToken ct = default);
    Task<TenantBilling?> GetWithSubscriptionsAsync(Guid tenantId, CancellationToken ct = default);
    Task<TenantBilling?> GetWithPaymentsAsync(Guid tenantId, CancellationToken ct = default);
    Task<TenantBilling?> GetFullAsync(Guid tenantId, CancellationToken ct = default);
    Task<IEnumerable<TenantBilling>> GetByStatusAsync(TenantStatus status, CancellationToken ct = default);
    Task<IEnumerable<TenantBilling>> GetExpiredSubscriptionsAsync(CancellationToken ct = default);
    Task<IEnumerable<TenantBilling>> GetPendingGracePeriodExpirationAsync(CancellationToken ct = default);
    Task<IEnumerable<TenantBilling>> GetPendingSuspensionAsync(int daysOverdue, CancellationToken ct = default);
    Task<IEnumerable<TenantBilling>> GetPendingDeletionAsync(int daysOverdue, CancellationToken ct = default);
}
