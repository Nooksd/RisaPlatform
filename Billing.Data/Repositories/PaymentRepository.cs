using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Billing.Data.Repositories;

public sealed class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(BillingDbContext context) : base(context)
    {
    }
    
    public async Task<Payment?> GetByGatewayPaymentIdAsync(string gatewayPaymentId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(p => p.GatewayPaymentId == gatewayPaymentId, ct);
        
    public async Task<IEnumerable<Payment>> GetByTenantBillingIdAsync(Guid tenantBillingId, CancellationToken ct = default)
        => await DbSet
            .Where(p => p.TenantBillingId == tenantBillingId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
            
    public async Task<IEnumerable<Payment>> GetByStatusAsync(PaymentStatus status, CancellationToken ct = default)
        => await DbSet.Where(p => p.Status == status).ToListAsync(ct);
        
    public async Task<Payment?> GetLatestByTenantBillingIdAsync(Guid tenantBillingId, CancellationToken ct = default)
        => await DbSet
            .Where(p => p.TenantBillingId == tenantBillingId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);
            
    public async Task<IEnumerable<Payment>> GetPendingExpiredAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        
        return await DbSet
            .Where(p => p.Status == PaymentStatus.Pending)
            .Where(p => 
                (p.Method == PaymentMethod.Pix && p.PixExpiresAt != null && p.PixExpiresAt < now) ||
                (p.Method == PaymentMethod.Boleto && p.DueDate != null && p.DueDate < now.AddDays(-3)))
            .ToListAsync(ct);
    }
}
