using Auth.Domain.Entities;
using Auth.Domain.Enums;
using Auth.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Auth.Data.Repositories;

public sealed class TenantAccountRepository(AuthDbContext context) : ITenantAccountRepository
{
    private readonly AuthDbContext _context = context;

    public async Task<TenantAccount?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.TenantAccounts
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<TenantAccount?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _context.TenantAccounts
            .FirstOrDefaultAsync(x => x.Email == email, ct);
    }

    public async Task<TenantAccount?> GetByOAuthAsync(string oauthId, OAuthProvider provider, CancellationToken ct = default)
    {
        return await _context.TenantAccounts
            .FirstOrDefaultAsync(x => x.OAuthId == oauthId && x.OAuthProvider == provider, ct);
    }

    public async Task<TenantAccount?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _context.TenantAccounts
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);
    }

    public async Task<bool> ExistsAsync(string email, CancellationToken ct = default)
    {
        return await _context.TenantAccounts
            .AnyAsync(x => x.Email == email, ct);
    }

    public async Task AddAsync(TenantAccount account, CancellationToken ct = default)
    {
        await _context.TenantAccounts.AddAsync(account, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TenantAccount account, CancellationToken ct = default)
    {
        _context.TenantAccounts.Update(account);
        await _context.SaveChangesAsync(ct);
    }
}