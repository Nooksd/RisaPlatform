using Billing.Domain.Entities;
using Billing.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Billing.Data.Repositories;

public sealed class SubscriptionRepository : Repository<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(BillingDbContext context) : base(context)
    {
    }
    
    public async Task<Subscription?> GetActiveByTenantBillingIdAsync(Guid tenantBillingId, CancellationToken ct = default)
        => await DbSet
            .Include(s => s.Modules)
            .Where(s => s.TenantBillingId == tenantBillingId)
            .Where(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.ExpiresAt)
            .FirstOrDefaultAsync(ct);
            
    public async Task<Subscription?> GetWithModulesAsync(Guid id, CancellationToken ct = default)
        => await DbSet
            .Include(s => s.Modules)
                .ThenInclude(m => m.Module)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
            
    public async Task<IEnumerable<Subscription>> GetByTenantBillingIdAsync(Guid tenantBillingId, CancellationToken ct = default)
        => await DbSet
            .Include(s => s.Modules)
            .Where(s => s.TenantBillingId == tenantBillingId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
            
    public async Task<IEnumerable<Subscription>> GetExpiringInDaysAsync(int days, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var target = now.AddDays(days);
        
        return await DbSet
            .Include(s => s.TenantBilling)
            .Include(s => s.Modules)
            .Where(s => s.IsActive)
            .Where(s => s.ExpiresAt > now && s.ExpiresAt <= target)
            .ToListAsync(ct);
    }
}
