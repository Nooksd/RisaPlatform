using Auth.Domain.Entities;
using Auth.Domain.Enums;

namespace Auth.Domain.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<IEnumerable<RefreshToken>> GetByUserAsync(Guid userId, AccountType accountType, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task UpdateAsync(RefreshToken token, CancellationToken ct = default);
    Task RevokeAllByUserAsync(Guid userId, AccountType accountType, string reason, CancellationToken ct = default);
    Task RevokeAllByTenantAsync(Guid tenantId, string reason, CancellationToken ct = default);
    Task DeleteExpiredAsync(CancellationToken ct = default);
}