using Auth.Domain.DTOs;
using Auth.Domain.Entities;
using Auth.Domain.Enums;
using Auth.Domain.Results;

namespace Auth.Domain.Interfaces.Services;

public interface ITenantUserService
{
    Task<AuthResult<TenantUser>> CreateAsync(Guid creatorId, AccountType creatorType, CreateTenantUserRequest request, CancellationToken ct = default);
    Task<AuthResult<TenantUser>> UpdateAsync(Guid userId, Guid editorId, AccountType editorType, UpdateTenantUserRequest request, CancellationToken ct = default);
    Task<AuthResult<bool>> ChangePasswordAsync(Guid targetUserId, Guid requesterId, AccountType requesterType, string newPassword, string? currentPassword = null, CancellationToken ct = default);
    Task<AuthResult<bool>> RevokeAccessAsync(Guid targetUserId, Guid requesterId, AccountType requesterType, CancellationToken ct = default);
    Task<AuthResult<IEnumerable<TenantUser>>> ListAsync(Guid tenantId, Guid requesterId, AccountType requesterType, CancellationToken ct = default);
    Task<AuthResult<TenantUser>> GetDetailAsync(Guid userId, Guid requesterId, AccountType requesterType, CancellationToken ct = default);
}