using Auth.Domain.Entities;
using Auth.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Auth.Data.Repositories;

public sealed class TenantRepository(AuthDbContext context) : ITenantRepository
{
    private readonly AuthDbContext _context = context;

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Tenants
            .Include(t => t.Users)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<Tenant?> GetByDomainAsync(string domain, CancellationToken ct = default)
    {
        return await _context.Tenants
            .Include(t => t.Users)
            .FirstOrDefaultAsync(x => x.Domain == domain, ct);
    }

    public async Task<IEnumerable<Tenant>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        return await _context.Tenants
            .Where(t => t.CreatedBy == ownerId)
            .Include(t => t.Users)
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsAsync(string domain, CancellationToken ct = default)
    {
        return await _context.Tenants
            .AnyAsync(x => x.Domain == domain, ct);
    }

    public async Task AddAsync(Tenant tenant, CancellationToken ct = default)
    {
        await _context.Tenants.AddAsync(tenant, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Tenant tenant, CancellationToken ct = default)
    {
        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync(ct);
    }
}