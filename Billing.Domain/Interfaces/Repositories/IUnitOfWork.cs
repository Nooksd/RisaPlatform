namespace Billing.Domain.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    IModuleRepository Modules { get; }
    ITenantBillingRepository TenantBillings { get; }
    ISubscriptionRepository Subscriptions { get; }
    IPaymentRepository Payments { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
