using Auth.Domain.DTOs;
using Auth.Domain.Entities;
using Auth.Domain.Enums;
using Auth.Domain.Interfaces.Repositories;
using Auth.Domain.Interfaces.Services;
using Auth.Domain.Results;
using Auth.Domain.ValueObjects;

namespace Auth.Api.Services;

public sealed class TenantUserService(
    ITenantAccountRepository tenantAccountRepo,
    ITenantUserRepository tenantUserRepo,
    IRefreshTokenRepository tokenRepo,
    IPasswordHasher passwordHasher) : ITenantUserService
{
    private readonly ITenantAccountRepository _tenantAccountRepo = tenantAccountRepo;
    private readonly ITenantUserRepository _tenantUserRepo = tenantUserRepo;
    private readonly IRefreshTokenRepository _tokenRepo = tokenRepo;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    public async Task<AuthResult<TenantUser>> CreateAsync(
        Guid creatorId,
        AccountType creatorType,
        CreateTenantUserRequest request,
        CancellationToken ct = default)
    {
        var (tenantIds, creatorAccesses) = await GetCreatorInfo(creatorId, creatorType, ct);

        if (!tenantIds.Contains(request.TenantId))
        {
            return AuthError.InsufficientPermissions;
        }

        if (request.TenantId == Guid.Empty)
            return AuthError.UserNotFound;

        var requestedAccesses = ModuleAccessCollection.Create(request.ModuleAccesses);

        if (creatorType == AccountType.TenantUser && !creatorAccesses.CanCreate(requestedAccesses))
            return AuthError.InsufficientPermissions;

        var email = Email.Create(request.Email);
        if (await _tenantUserRepo.ExistsAsync(request.TenantId, email, request.Username, ct))
            return AuthError.UserAlreadyExists;

        var passwordHash = PasswordHash.Create(_passwordHasher.Hash(request.Password));
        var user = TenantUser.Create(request.TenantId, email, request.Username, passwordHash, request.Name, creatorId, requestedAccesses);

        await _tenantUserRepo.AddAsync(user, ct);

        return user;
    }

    public async Task<AuthResult<TenantUser>> UpdateAsync(
        Guid userId,
        Guid editorId,
        AccountType editorType,
        UpdateTenantUserRequest request,
        CancellationToken ct = default)
    {
        var user = await _tenantUserRepo.GetByIdAsync(userId, ct);
        if (user is null)
            return AuthError.UserNotFound;

        if (editorType == AccountType.TenantOwner)
        {
            var owner = await _tenantAccountRepo.GetByIdAsync(editorId, ct);

            var tenantIds = owner!.Tenants.Select(t => t.Id).ToList();

            if (owner is null || !tenantIds.Contains(user.TenantId))
                return AuthError.InsufficientPermissions;
        }
        else if (editorType == AccountType.TenantUser)
        {
            var editor = await _tenantUserRepo.GetByIdAsync(editorId, ct);

            if (editor is null || editor.TenantId != user.TenantId)
                return AuthError.InsufficientPermissions;

            var editorAccesses = editor.GetModuleAccesses();
            var requestedAccesses = ModuleAccessCollection.Create(request.ModuleAccesses);

            if (!editorAccesses.CanCreate(requestedAccesses))
                return AuthError.InsufficientPermissions;
        }
        else
        {
            return AuthError.InsufficientPermissions;
        }

        user.UpdateName(request.Name);
        user.UpdateModuleAccesses(ModuleAccessCollection.Create(request.ModuleAccesses));

        await _tenantUserRepo.UpdateAsync(user, ct);

        return user;
    }

    public async Task<AuthResult<bool>> ChangePasswordAsync(
        Guid targetUserId,
        Guid requesterId,
        AccountType requesterType,
        string newPassword,
        string? currentPassword = null,
        CancellationToken ct = default)
    {
        var targetUser = await _tenantUserRepo.GetByIdAsync(targetUserId, ct);
        if (targetUser is null)
            return AuthError.UserNotFound;

        if (targetUserId == requesterId && requesterType == AccountType.TenantUser)
        {
            var hasAdminAccess = targetUser.ModuleAccesses.Any(ma => ma.AccessLevel == 3);

            if (!hasAdminAccess)
            {
                if (string.IsNullOrWhiteSpace(currentPassword))
                    return AuthError.Custom("PASSWORD_REQUIRED", "Current password is required");

                if (!_passwordHasher.Verify(currentPassword, targetUser.PasswordHash))
                    return AuthError.InvalidCredentials;
            }
        }
        else if (requesterType == AccountType.TenantOwner)
        {
            var owner = await _tenantAccountRepo.GetByIdAsync(requesterId, ct);

            var tenantIds = owner!.Tenants.Select(t => t.Id).ToList();

            if (owner is null || !tenantIds.Contains(targetUser.TenantId))
                return AuthError.InsufficientPermissions;
        }
        else if (requesterType == AccountType.TenantUser)
        {
            var requester = await _tenantUserRepo.GetByIdAsync(requesterId, ct);

            if (requester is null || requester.TenantId != targetUser.TenantId)
                return AuthError.InsufficientPermissions;

            var hasAdminAccess = requester.ModuleAccesses.Any(ma => ma.AccessLevel == 3);
            if (!hasAdminAccess)
                return AuthError.InsufficientPermissions;
        }
        else
        {
            return AuthError.InsufficientPermissions;
        }

        var newPasswordHash = PasswordHash.Create(_passwordHasher.Hash(newPassword));
        targetUser.UpdatePassword(newPasswordHash);

        await _tenantUserRepo.UpdateAsync(targetUser, ct);
        await _tokenRepo.RevokeByUserAsync(targetUserId, AccountType.TenantUser, "Password changed", ct);

        return true;
    }

    public async Task<AuthResult<bool>> RevokeAccessAsync(
        Guid targetUserId,
        Guid requesterId,
        AccountType requesterType,
        CancellationToken ct = default)
    {
        var targetUser = await _tenantUserRepo.GetByIdAsync(targetUserId, ct);
        if (targetUser is null)
            return AuthError.UserNotFound;

        if (requesterType == AccountType.TenantOwner)
        {
            var owner = await _tenantAccountRepo.GetByIdAsync(requesterId, ct);

            var tenantIds = owner!.Tenants.Select(t => t.Id).ToList();

            if (owner is null || !tenantIds.Contains(targetUser.TenantId))
                return AuthError.InsufficientPermissions;
        }
        else if (requesterType == AccountType.TenantUser)
        {
            var requester = await _tenantUserRepo.GetByIdAsync(requesterId, ct);
            if (requester is null || requester.TenantId != targetUser.TenantId)
                return AuthError.InsufficientPermissions;

            var hasAdminAccess = requester.ModuleAccesses.Any(ma => ma.AccessLevel == 3);
            if (!hasAdminAccess)
                return AuthError.InsufficientPermissions;
        }
        else
        {
            return AuthError.InsufficientPermissions;
        }

        await _tokenRepo.RevokeByUserAsync(targetUserId, AccountType.TenantUser, "Revoked by admin", ct);

        return true;
    }

    public async Task<AuthResult<IEnumerable<TenantUser>>> ListAsync(
        Guid tenantId,
        Guid requesterId,
        AccountType requesterType,
        CancellationToken ct = default)
    {
        if (requesterType == AccountType.TenantOwner)
        {
            var owner = await _tenantAccountRepo.GetByIdAsync(requesterId, ct);

            var tenantIds = owner!.Tenants.Select(t => t.Id).ToList();

            if (owner is null || !tenantIds.Contains(tenantId))
                return AuthError.InsufficientPermissions;
        }
        else if (requesterType == AccountType.TenantUser)
        {
            var requester = await _tenantUserRepo.GetByIdAsync(requesterId, ct);
            if (requester is null || requester.TenantId != tenantId)
                return AuthError.InsufficientPermissions;

            var hasAdminAccess = requester.ModuleAccesses.Any(ma => ma.AccessLevel == 3);
            if (!hasAdminAccess)
                return AuthError.InsufficientPermissions;
        }
        else
        {
            return AuthError.InsufficientPermissions;
        }

        var users = await _tenantUserRepo.GetByTenantIdAsync(tenantId, ct);
        var response = users;
        return AuthResult<IEnumerable<TenantUser>>.Success(response);
    }

    public async Task<AuthResult<TenantUser>> GetDetailAsync(
        Guid userId,
        Guid requesterId,
        AccountType requesterType,
        CancellationToken ct = default)
    {
        var user = await _tenantUserRepo.GetByIdAsync(userId, ct);
        if (user is null)
            return AuthError.UserNotFound;

        if (requesterType == AccountType.TenantOwner)
        {
            var owner = await _tenantAccountRepo.GetByIdAsync(requesterId, ct);

            var tenantIds = owner!.Tenants.Select(t => t.Id).ToList();

            if (owner is null || !tenantIds.Contains(user.TenantId))
                return AuthError.InsufficientPermissions;
        }
        else if (requesterType == AccountType.TenantUser)
        {
            var requester = await _tenantUserRepo.GetByIdAsync(requesterId, ct);
            if (requester is null || requester.TenantId != user.TenantId)
                return AuthError.InsufficientPermissions;

            if (requester.Id != userId)
            {
                var hasAdminAccess = requester.ModuleAccesses.Any(ma => ma.AccessLevel == 3);
                if (!hasAdminAccess)
                    return AuthError.InsufficientPermissions;
            }
        }
        else
        {
            return AuthError.InsufficientPermissions;
        }

        return user;
    }

    private async Task<(List<Guid> TenantIds, ModuleAccessCollection Accesses)> GetCreatorInfo(
        Guid creatorId,
        AccountType creatorType,
        CancellationToken ct)
    {
        if (creatorType == AccountType.TenantOwner)
        {
            var owner = await _tenantAccountRepo.GetByIdAsync(creatorId, ct);

            List<Guid> tenantIds = owner!.Tenants.Select(t => t.Id).ToList();

            return owner is not null
                ? (tenantIds, ModuleAccessCollection.FullAccess())
                : ([], ModuleAccessCollection.Empty());
        }

        if (creatorType == AccountType.TenantUser)
        {
            var user = await _tenantUserRepo.GetByIdAsync(creatorId, ct);

            var tenantIds = user is not null ? new List<Guid> { user.TenantId } : [];

            return user is not null
                ? (tenantIds, user.GetModuleAccesses())
                : ([], ModuleAccessCollection.Empty());
        }

        return ([], ModuleAccessCollection.Empty());
    }
}