using Auth.Domain.Entities;

namespace Auth.Domain.Interfaces.Repositories;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Tenant?> GetByDomainAsync(string domain, CancellationToken ct = default);
    Task<IEnumerable<Tenant>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<bool> ExistsAsync(string domain, CancellationToken ct = default);
    Task AddAsync(Tenant tenant, CancellationToken ct = default);
    Task UpdateAsync(Tenant tenant, CancellationToken ct = default);
}