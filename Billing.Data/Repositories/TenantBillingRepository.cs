using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Billing.Data.Repositories;

public sealed class TenantBillingRepository : Repository<TenantBilling>, ITenantBillingRepository
{
    public TenantBillingRepository(BillingDbContext context) : base(context)
    {
    }
    
    public async Task<TenantBilling?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(t => t.TenantId == tenantId, ct);
        
    public async Task<TenantBilling?> GetByTenantAccountIdAsync(Guid tenantAccountId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(t => t.TenantAccountId == tenantAccountId, ct);
        
    public async Task<TenantBilling?> GetWithSubscriptionsAsync(Guid tenantId, CancellationToken ct = default)
        => await DbSet
            .Include(t => t.Subscriptions)
                .ThenInclude(s => s.Modules)
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, ct);
            
    public async Task<TenantBilling?> GetWithPaymentsAsync(Guid tenantId, CancellationToken ct = default)
        => await DbSet
            .Include(t => t.Payments.OrderByDescending(p => p.CreatedAt))
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, ct);
            
    public async Task<TenantBilling?> GetFullAsync(Guid tenantId, CancellationToken ct = default)
        => await DbSet
            .Include(t => t.Subscriptions)
                .ThenInclude(s => s.Modules)
            .Include(t => t.Payments.OrderByDescending(p => p.CreatedAt))
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, ct);
            
    public async Task<IEnumerable<TenantBilling>> GetByStatusAsync(TenantStatus status, CancellationToken ct = default)
        => await DbSet.Where(t => t.Status == status).ToListAsync(ct);
        
    public async Task<IEnumerable<TenantBilling>> GetExpiredSubscriptionsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        
        return await DbSet
            .Include(t => t.Subscriptions)
            .Where(t => t.Status == TenantStatus.Active)
            .Where(t => !t.Subscriptions.Any(s => s.IsActive && s.ExpiresAt > now))
            .ToListAsync(ct);
    }
    
    public async Task<IEnumerable<TenantBilling>> GetPendingGracePeriodExpirationAsync(CancellationToken ct = default)
    {
        var gracePeriodDuration = TimeSpan.FromDays(5);
        var cutoff = DateTime.UtcNow.Subtract(gracePeriodDuration);
        
        return await DbSet
            .Where(t => t.Status == TenantStatus.GracePeriod)
            .Where(t => t.GracePeriodRequestedAt != null && t.GracePeriodRequestedAt <= cutoff)
            .ToListAsync(ct);
    }
    
    public async Task<IEnumerable<TenantBilling>> GetPendingSuspensionAsync(int daysOverdue, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysOverdue);
        
        return await DbSet
            .Include(t => t.Subscriptions)
            .Where(t => t.Status == TenantStatus.Active || t.Status == TenantStatus.GracePeriod)
            .Where(t => !t.Subscriptions.Any(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow))
            .Where(t => t.StatusChangedAt <= cutoff)
            .ToListAsync(ct);
    }
    
    public async Task<IEnumerable<TenantBilling>> GetPendingDeletionAsync(int daysOverdue, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysOverdue);
        
        return await DbSet
            .Where(t => t.Status == TenantStatus.Suspended || t.Status == TenantStatus.PendingDeletion)
            .Where(t => t.StatusChangedAt <= cutoff)
            .ToListAsync(ct);
    }
}
