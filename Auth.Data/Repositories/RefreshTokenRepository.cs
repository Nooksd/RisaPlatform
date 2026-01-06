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

    public async Task<RefreshToken?> GetLatestByUserAsync(Guid userId, AccountType accountType, CancellationToken ct = default)
    {
        return await _context.RefreshTokens
            .Where(x => x.UserId == userId && x.AccountType == accountType)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        var previousTokens = await _context.RefreshTokens
            .Where(x => x.UserId == token.UserId &&
                       x.AccountType == token.AccountType &&
                       !x.IsRevoked)
            .ToListAsync(ct);

        foreach (var prevToken in previousTokens)
        {
            prevToken.Revoke("Replaced by new token");
        }

        await _context.RefreshTokens.AddAsync(token, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        _context.RefreshTokens.Update(token);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RevokeByUserAsync(Guid userId, AccountType accountType, string reason, CancellationToken ct = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(x => x.UserId == userId &&
                       x.AccountType == accountType &&
                       !x.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.Revoke(reason);
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task RevokeTokenAsync(string token, string reason, CancellationToken ct = default)
    {
        var refreshToken = await GetByTokenAsync(token, ct);
        if (refreshToken is not null)
        {
            refreshToken.Revoke(reason);
            await UpdateAsync(refreshToken, ct);
        }
    }

    public async Task CleanupExpiredTokensAsync(CancellationToken ct = default)
    {
        var expiredTokens = await _context.RefreshTokens
            .Where(x => x.ExpiresAt < DateTime.UtcNow.AddDays(-7)) // Limpa tokens expirados há mais de 7 dias
            .ToListAsync(ct);

        _context.RefreshTokens.RemoveRange(expiredTokens);
        await _context.SaveChangesAsync(ct);
    }
}