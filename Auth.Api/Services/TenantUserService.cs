using Auth.Api.DTOs;
using Auth.Domain.Entities;
using Auth.Domain.Enums;
using Auth.Domain.Interfaces.Repositories;
using Auth.Domain.Interfaces.Services;
using Auth.Domain.Results;
using Auth.Domain.ValueObjects;
using AutoMapper;

namespace Auth.Api.Services;

public interface ITenantUserService
{
    Task<AuthResult<TenantUserResponse>> CreateAsync(Guid creatorId, AccountType creatorType, CreateTenantUserRequest request, CancellationToken ct = default);
    Task<AuthResult<TenantUserResponse>> UpdateAsync(Guid userId, Guid editorId, AccountType editorType, UpdateTenantUserRequest request, CancellationToken ct = default);
    Task<AuthResult<bool>> ChangePasswordAsync(Guid targetUserId, Guid requesterId, AccountType requesterType, ChangePasswordRequest request, CancellationToken ct = default);
    Task<AuthResult<bool>> RevokeTokensAsync(Guid targetUserId, Guid requesterId, AccountType requesterType, CancellationToken ct = default);
    Task<AuthResult<IEnumerable<TenantUserResponse>>> ListAsync(Guid tenantId, Guid requesterId, AccountType requesterType, CancellationToken ct = default);
    Task<AuthResult<TenantUserDetailResponse>> GetDetailAsync(Guid userId, Guid requesterId, AccountType requesterType, CancellationToken ct = default);
}

public sealed class TenantUserService : ITenantUserService
{
    private readonly ITenantAccountRepository _tenantAccountRepo;
    private readonly ITenantUserRepository _tenantUserRepo;
    private readonly IRefreshTokenRepository _tokenRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;

    public TenantUserService(
        ITenantAccountRepository tenantAccountRepo,
        ITenantUserRepository tenantUserRepo,
        IRefreshTokenRepository tokenRepo,
        IPasswordHasher passwordHasher,
        IMapper mapper)
    {
        _tenantAccountRepo = tenantAccountRepo;
        _tenantUserRepo = tenantUserRepo;
        _tokenRepo = tokenRepo;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
    }

    public async Task<AuthResult<TenantUserResponse>> CreateAsync(
        Guid creatorId,
        AccountType creatorType,
        CreateTenantUserRequest request,
        CancellationToken ct = default)
    {
        // Busca o criador para validar permissões
        var (tenantId, creatorAccesses) = await GetCreatorInfo(creatorId, creatorType, ct);
        if (tenantId == Guid.Empty)
            return AuthError.UserNotFound;

        // Valida se o criador tem permissão para criar com esses acessos
        var requestedAccesses = ModuleAccessCollection.Create(request.ModuleAccesses);

        if (creatorType == AccountType.TenantUser && !creatorAccesses.CanCreate(requestedAccesses))
            return AuthError.InsufficientPermissions;

        // Verifica se já existe usuário com esse email no tenant
        var email = Email.Create(request.Email);
        if (await _tenantUserRepo.ExistsAsync(tenantId, email, ct))
            return AuthError.UserAlreadyExists;

        // Cria o usuário
        var passwordHash = PasswordHash.Create(_passwordHasher.Hash(request.Password));
        var user = TenantUser.Create(tenantId, email, passwordHash, request.Name, creatorId, requestedAccesses);

        await _tenantUserRepo.AddAsync(user, ct);

        return _mapper.Map<TenantUserResponse>(user);
    }

    public async Task<AuthResult<TenantUserResponse>> UpdateAsync(
        Guid userId,
        Guid editorId,
        AccountType editorType,
        UpdateTenantUserRequest request,
        CancellationToken ct = default)
    {
        var user = await _tenantUserRepo.GetByIdAsync(userId, ct);
        if (user is null)
            return AuthError.UserNotFound;

        // TenantOwner pode editar qualquer um
        if (editorType == AccountType.TenantOwner)
        {
            var owner = await _tenantAccountRepo.GetByIdAsync(editorId, ct);
            if (owner is null || owner.TenantId != user.TenantId)
                return AuthError.InsufficientPermissions;
        }
        // TenantUser só pode editar se tiver permissão
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

        return _mapper.Map<TenantUserResponse>(user);
    }

    public async Task<AuthResult<bool>> ChangePasswordAsync(
        Guid targetUserId,
        Guid requesterId,
        AccountType requesterType,
        ChangePasswordRequest request,
        CancellationToken ct = default)
    {
        var targetUser = await _tenantUserRepo.GetByIdAsync(targetUserId, ct);
        if (targetUser is null)
            return AuthError.UserNotFound;

        // Se é o próprio usuário alterando
        if (targetUserId == requesterId && requesterType == AccountType.TenantUser)
        {
            // Verifica se tem acesso 3 em algum módulo
            var hasAdminAccess = targetUser.ModuleAccesses.Any(ma => ma.AccessLevel == 3);

            // Se não tem acesso admin, precisa fornecer senha atual
            if (!hasAdminAccess)
            {
                if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                    return AuthError.Custom("PASSWORD_REQUIRED", "Current password is required");

                if (!_passwordHasher.Verify(request.CurrentPassword, targetUser.PasswordHash))
                    return AuthError.InvalidCredentials;
            }
        }
        // TenantOwner pode alterar de qualquer um do seu tenant
        else if (requesterType == AccountType.TenantOwner)
        {
            var owner = await _tenantAccountRepo.GetByIdAsync(requesterId, ct);
            if (owner is null || owner.TenantId != targetUser.TenantId)
                return AuthError.InsufficientPermissions;
        }
        // TenantUser com acesso 3 em pelo menos 1 módulo pode alterar de outros
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

        var newPasswordHash = PasswordHash.Create(_passwordHasher.Hash(request.NewPassword));
        targetUser.UpdatePassword(newPasswordHash);

        await _tenantUserRepo.UpdateAsync(targetUser, ct);

        // Revoga todos os refresh tokens do usuário para forçar novo login
        await _tokenRepo.RevokeAllByUserAsync(targetUserId, AccountType.TenantUser, "Password changed", ct);

        return true;
    }

    public async Task<AuthResult<bool>> RevokeTokensAsync(
        Guid targetUserId,
        Guid requesterId,
        AccountType requesterType,
        CancellationToken ct = default)
    {
        var targetUser = await _tenantUserRepo.GetByIdAsync(targetUserId, ct);
        if (targetUser is null)
            return AuthError.UserNotFound;

        // TenantOwner pode revogar de qualquer um do seu tenant
        if (requesterType == AccountType.TenantOwner)
        {
            var owner = await _tenantAccountRepo.GetByIdAsync(requesterId, ct);
            if (owner is null || owner.TenantId != targetUser.TenantId)
                return AuthError.InsufficientPermissions;
        }
        // TenantUser com acesso 3 em pelo menos 1 módulo pode revogar de outros
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

        await _tokenRepo.RevokeAllByUserAsync(targetUserId, AccountType.TenantUser, "Revoked by admin", ct);

        return true;
    }

    public async Task<AuthResult<IEnumerable<TenantUserResponse>>> ListAsync(
        Guid tenantId,
        Guid requesterId,
        AccountType requesterType,
        CancellationToken ct = default)
    {
        // Verifica se tem permissão (acesso 3 em pelo menos 1 módulo)
        if (requesterType == AccountType.TenantOwner)
        {
            var owner = await _tenantAccountRepo.GetByIdAsync(requesterId, ct);
            if (owner is null || owner.TenantId != tenantId)
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
        var response = _mapper.Map<IEnumerable<TenantUserResponse>>(users);
        return AuthResult<IEnumerable<TenantUserResponse>>.Success(response);
    }

    public async Task<AuthResult<TenantUserDetailResponse>> GetDetailAsync(
        Guid userId,
        Guid requesterId,
        AccountType requesterType,
        CancellationToken ct = default)
    {
        var user = await _tenantUserRepo.GetByIdAsync(userId, ct);
        if (user is null)
            return AuthError.UserNotFound;

        // Verifica se tem permissão
        if (requesterType == AccountType.TenantOwner)
        {
            var owner = await _tenantAccountRepo.GetByIdAsync(requesterId, ct);
            if (owner is null || owner.TenantId != user.TenantId)
                return AuthError.InsufficientPermissions;
        }
        else if (requesterType == AccountType.TenantUser)
        {
            var requester = await _tenantUserRepo.GetByIdAsync(requesterId, ct);
            if (requester is null || requester.TenantId != user.TenantId)
                return AuthError.InsufficientPermissions;

            // Pode ver o próprio ou se tiver acesso 3
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

        return _mapper.Map<TenantUserDetailResponse>(user);
    }

    private async Task<(Guid TenantId, ModuleAccessCollection Accesses)> GetCreatorInfo(
        Guid creatorId,
        AccountType creatorType,
        CancellationToken ct)
    {
        if (creatorType == AccountType.TenantOwner)
        {
            var owner = await _tenantAccountRepo.GetByIdAsync(creatorId, ct);
            return owner is not null
                ? (owner.TenantId, ModuleAccessCollection.FullAccess())
                : (Guid.Empty, ModuleAccessCollection.Empty());
        }

        if (creatorType == AccountType.TenantUser)
        {
            var user = await _tenantUserRepo.GetByIdAsync(creatorId, ct);
            return user is not null
                ? (user.TenantId, user.GetModuleAccesses())
                : (Guid.Empty, ModuleAccessCollection.Empty());
        }

        return (Guid.Empty, ModuleAccessCollection.Empty());
    }
}