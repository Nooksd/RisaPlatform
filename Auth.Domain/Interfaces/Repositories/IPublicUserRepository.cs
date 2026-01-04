using Auth.Domain.Entities;
using Auth.Domain.Enums;

namespace Auth.Domain.Interfaces.Repositories;

public interface IPublicUserRepository
{
    Task<PublicUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PublicUser?> GetByEmailAsync(Guid tenantId, string module, string email, CancellationToken ct = default);
    Task<PublicUser?> GetByOAuthAsync(Guid tenantId, string module, string oauthId, OAuthProvider provider, CancellationToken ct = default);
    Task<IEnumerable<PublicUser>> GetByTenantAndModuleAsync(Guid tenantId, string module, bool includeDeleted = false, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid tenantId, string module, string email, CancellationToken ct = default);
    Task AddAsync(PublicUser user, CancellationToken ct = default);
    Task UpdateAsync(PublicUser user, CancellationToken ct = default);
    Task<int> CountByTenantAndModuleAsync(Guid tenantId, string module, bool includeDeleted = false, CancellationToken ct = default);
}
