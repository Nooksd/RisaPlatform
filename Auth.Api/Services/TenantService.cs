
using Auth.Domain.DTOs;
using Auth.Domain.Entities;
using Auth.Domain.Interfaces.Repositories;
using Auth.Domain.Interfaces.Services;
using Auth.Domain.Results;

namespace Auth.Api.Services;

public sealed class TenantService(
    ITenantRepository tenantRepo,
    ITenantAccountRepository tenantAccountRepo) : ITenantService
{
    private readonly ITenantRepository _tenantRepo = tenantRepo;
    private readonly ITenantAccountRepository _tenantAccountRepo = tenantAccountRepo;

    public async Task<AuthResult<Tenant>> CreateAsync(
        Guid ownerId,
        CreateTenantRequest request,
        CancellationToken ct = default)
    {
        var owner = await _tenantAccountRepo.GetByIdAsync(ownerId, ct);
        if (owner is null)
            return AuthError.UserNotFound;

        var domain = Domain.ValueObjects.Domain.Create(request.Domain);

        if (await _tenantRepo.ExistsAsync(domain, ct))
            return AuthError.Custom("DOMAIN_EXISTS", "Domain already in use");

        var tenant = Tenant.Create(domain, request.Name, ownerId);

        await _tenantRepo.AddAsync(tenant, ct);

        return tenant;
    }

    public async Task<AuthResult<Tenant>> UpdateAsync(
        Guid tenantId,
        Guid ownerId,
        UpdateTenantRequest request,
        CancellationToken ct = default)
    {
        var tenant = await _tenantRepo.GetByIdAsync(tenantId, ct);
        if (tenant is null)
            return AuthError.TenantNotFound;

        if (tenant.CreatedBy != ownerId)
            return AuthError.InsufficientPermissions;

        if (request.Domain is not null)
        {
            var newDomain = Domain.ValueObjects.Domain.Create(request.Domain);

            var existingTenant = await _tenantRepo.GetByDomainAsync(newDomain, ct);
            if (existingTenant is not null && existingTenant.Id != tenantId)
                return AuthError.Custom("DOMAIN_EXISTS", "Domain already in use");

            tenant.UpdateDomain(newDomain);
        }

        if (request.Name is not null)
        {
            tenant.UpdateName(request.Name);
        }

        await _tenantRepo.UpdateAsync(tenant, ct);

        return tenant;
    }

    public async Task<AuthResult<Tenant>> GetByDomainAsync(
        string domain,
        CancellationToken ct = default)
    {
        var domainValue = Domain.ValueObjects.Domain.Create(domain);
        var tenant = await _tenantRepo.GetByDomainAsync(domainValue, ct);
        if (tenant is null)
            return AuthError.TenantNotFound;

        return tenant;
    }

    public async Task<AuthResult<IEnumerable<Tenant>>> ListByOwnerAsync(
        Guid ownerId,
        CancellationToken ct = default)
    {
        var owner = await _tenantAccountRepo.GetByIdAsync(ownerId, ct);
        if (owner is null)
            return AuthError.UserNotFound;

        return AuthResult<IEnumerable<Tenant>>.Success(owner.Tenants);
    }

    public async Task<AuthResult<Tenant>> GetByIdAsync(
        Guid tenantId,
        Guid requesterId,
        CancellationToken ct = default)
    {
        var tenant = await _tenantRepo.GetByIdAsync(tenantId, ct);
        if (tenant is null)
            return AuthError.TenantNotFound;

        if (tenant.CreatedBy != requesterId && !tenant.Users.Any(u => u.Id == requesterId))
            return AuthError.InsufficientPermissions;

        return tenant;
    }
}