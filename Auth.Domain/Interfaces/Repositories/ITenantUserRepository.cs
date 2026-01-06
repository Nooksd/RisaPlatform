using Auth.Domain.Entities;

namespace Auth.Domain.Interfaces.Repositories;

public interface ITenantUserRepository
{
    Task<TenantUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TenantUser?> GetByEmailOrUsernameAsync(Guid tenantId, string? email, string? username, CancellationToken ct = default);
    Task<IEnumerable<TenantUser>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid tenantId, string email, string username,CancellationToken ct = default);
    Task AddAsync(TenantUser user, CancellationToken ct = default);
    Task UpdateAsync(TenantUser user, CancellationToken ct = default);
    Task<int> CountByTenantAsync(Guid tenantId, CancellationToken ct = default);
}
