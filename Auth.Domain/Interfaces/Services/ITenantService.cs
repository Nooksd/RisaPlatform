using Auth.Domain.DTOs;
using Auth.Domain.Entities;
using Auth.Domain.Results;

namespace Auth.Domain.Interfaces.Services;

public interface ITenantService
{
    Task<AuthResult<Tenant>> CreateAsync(Guid ownerId, CreateTenantRequest request, CancellationToken ct = default);
    Task<AuthResult<Tenant>> UpdateAsync(Guid tenantId, Guid ownerId, UpdateTenantRequest request, CancellationToken ct = default);
    Task<AuthResult<Tenant>> GetByDomainAsync(string domain, CancellationToken ct = default);
    Task<AuthResult<IEnumerable<Tenant>>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<AuthResult<Tenant>> GetByIdAsync(Guid tenantId, Guid requesterId, CancellationToken ct = default);
}