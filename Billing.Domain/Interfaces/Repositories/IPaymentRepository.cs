using Billing.Domain.Entities;
using Billing.Domain.Enums;

namespace Billing.Domain.Interfaces.Repositories;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetByGatewayPaymentIdAsync(string gatewayPaymentId, CancellationToken ct = default);
    Task<IEnumerable<Payment>> GetByTenantBillingIdAsync(Guid tenantBillingId, CancellationToken ct = default);
    Task<IEnumerable<Payment>> GetByStatusAsync(PaymentStatus status, CancellationToken ct = default);
    Task<Payment?> GetLatestByTenantBillingIdAsync(Guid tenantBillingId, CancellationToken ct = default);
    Task<IEnumerable<Payment>> GetPendingExpiredAsync(CancellationToken ct = default);
}
