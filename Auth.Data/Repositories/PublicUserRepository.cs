using Auth.Domain.Entities;
using Auth.Domain.Enums;
using Auth.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Auth.Data.Repositories;

public sealed class PublicUserRepository(AuthDbContext context) : IPublicUserRepository
{
    private readonly AuthDbContext _context = context;

    public async Task<PublicUser?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.PublicUsers
            
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<PublicUser?> GetByEmailAsync(Guid tenantId, string module, string email, CancellationToken ct = default)
    {
        return await _context.PublicUsers
            
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.Module == module &&
                x.Email == email, ct);
    }

    public async Task<PublicUser?> GetByOAuthAsync(Guid tenantId, string module, string oauthId, OAuthProvider provider, CancellationToken ct = default)
    {
        return await _context.PublicUsers
            
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.Module == module &&
                x.OAuthId == oauthId &&
                x.OAuthProvider == provider, ct);
    }

    public async Task<IEnumerable<PublicUser>> GetByTenantAndModuleAsync(Guid tenantId, string module, bool includeDeleted = false, CancellationToken ct = default)
    {
        var query = _context.PublicUsers
            .Where(x => x.TenantId == tenantId && x.Module == module);

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.ToListAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid tenantId, string module, string email, CancellationToken ct = default)
    {
        return await _context.PublicUsers
            .AnyAsync(x =>
                x.TenantId == tenantId &&
                x.Module == module &&
                x.Email == email, ct);
    }

    public async Task AddAsync(PublicUser user, CancellationToken ct = default)
    {
        await _context.PublicUsers.AddAsync(user, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(PublicUser user, CancellationToken ct = default)
    {
        _context.PublicUsers.Update(user);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<int> CountByTenantAndModuleAsync(Guid tenantId, string module, bool includeDeleted = false, CancellationToken ct = default)
    {
        var query = _context.PublicUsers
            .Where(x => x.TenantId == tenantId && x.Module == module);

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.CountAsync(ct);
    }
}