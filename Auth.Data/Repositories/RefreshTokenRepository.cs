using Auth.Domain.Entities;
using Auth.Domain.Enums;
using Auth.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Auth.Data.Repositories;

public sealed class RefreshTokenRepository(AuthDbContext context) : IRefreshTokenRepository
{
    private readonly AuthDbContext _context = context;

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == token, ct);
    }

    public async Task<IEnumerable<RefreshToken>> GetByUserAsync(Guid userId, AccountType accountType, CancellationToken ct = default)
    {
        return await _context.RefreshTokens
            .Where(x => x.UserId == userId && x.AccountType == accountType)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        await _context.RefreshTokens.AddAsync(token, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        _context.RefreshTokens.Update(token);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RevokeAllByUserAsync(Guid userId, AccountType accountType, string reason, CancellationToken ct = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(x => x.UserId == userId && x.AccountType == accountType && !x.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.Revoke(reason);
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task RevokeAllByTenantAsync(Guid tenantId, string reason, CancellationToken ct = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(x => x.TenantId == tenantId && !x.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.Revoke(reason);
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteExpiredAsync(CancellationToken ct = default)
    {
        var expiredTokens = await _context.RefreshTokens
            .Where(x => x.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(ct);

        _context.RefreshTokens.RemoveRange(expiredTokens);
        await _context.SaveChangesAsync(ct);
    }
}