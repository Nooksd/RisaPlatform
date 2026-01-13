using Billing.Domain.Entities;
using Billing.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Billing.Data.Repositories;

public sealed class ModuleRepository : Repository<Module>, IModuleRepository
{
    public ModuleRepository(BillingDbContext context) : base(context)
    {
    }
    
    public async Task<Module?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await DbSet
            .Include(m => m.QuantityDiscounts)
            .FirstOrDefaultAsync(m => m.Code == code.ToUpperInvariant(), ct);
            
    public async Task<IEnumerable<Module>> GetActiveModulesAsync(CancellationToken ct = default)
        => await DbSet
            .Include(m => m.QuantityDiscounts)
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .ToListAsync(ct);
            
    public async Task<IEnumerable<Module>> GetByCodesAsync(IEnumerable<string> codes, CancellationToken ct = default)
    {
        var upperCodes = codes.Select(c => c.ToUpperInvariant()).ToList();
        return await DbSet
            .Include(m => m.QuantityDiscounts)
            .Where(m => upperCodes.Contains(m.Code))
            .ToListAsync(ct);
    }
    
    public async Task<Module?> GetWithQuantityDiscountsAsync(Guid id, CancellationToken ct = default)
        => await DbSet
            .Include(m => m.QuantityDiscounts.OrderBy(d => d.MinUsers))
            .FirstOrDefaultAsync(m => m.Id == id, ct);
}
