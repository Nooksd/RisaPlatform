using Auth.Domain.Entities;
using Auth.Domain.Enums;

namespace Auth.Domain.Interfaces.Repositories;

public interface ITenantAccountRepository
{
    Task<TenantAccount?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TenantAccount?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<TenantAccount?> GetByOAuthAsync(string oauthId, OAuthProvider provider, CancellationToken ct = default);
    Task<TenantAccount?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<bool> ExistsAsync(string email, CancellationToken ct = default);
    Task AddAsync(TenantAccount account, CancellationToken ct = default);
    Task UpdateAsync(TenantAccount account, CancellationToken ct = default);
}