using Auth.Domain.Entities;
using Auth.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Auth.Data.Repositories;

public sealed class TenantUserRepository(AuthDbContext context) : ITenantUserRepository
{
    private readonly AuthDbContext _context = context;

    public async Task<TenantUser?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.TenantUsers
            .Include(x => x.ModuleAccesses)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<TenantUser?> GetByEmailOrUsernameAsync(Guid tenantId, string? email, string? username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var query = _context.TenantUsers
            .Include(x => x.ModuleAccesses)
            .Where(x => x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(email))
        {
            query = query.Where(x => x.Email == email);
        }
        else if (!string.IsNullOrWhiteSpace(username))
        {
            query = query.Where(x => x.Username == username);
        }

        return await query.FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<TenantUser>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _context.TenantUsers
            .Include(x => x.ModuleAccesses)
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid tenantId, string email, string username, CancellationToken ct = default)
    {
        return await _context.TenantUsers
            .AnyAsync(x => x.TenantId == tenantId && (x.Email == email || x.Username == username), ct);
    }

    public async Task AddAsync(TenantUser user, CancellationToken ct = default)
    {
        await _context.TenantUsers.AddAsync(user, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TenantUser user, CancellationToken ct = default)
    {
        _context.TenantUsers.Update(user);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<int> CountByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _context.TenantUsers
            .CountAsync(x => x.TenantId == tenantId, ct);
    }
}
