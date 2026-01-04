using Auth.Api.DTOs;
using Auth.Domain.Entities;
using Auth.Domain.Enums;
using Auth.Domain.Interfaces.Repositories;
using Auth.Domain.Interfaces.Services;
using Auth.Domain.Results;
using Auth.Domain.ValueObjects;
using AutoMapper;

namespace Auth.Api.Services;

public interface IAuthService
{
    // Tenant Account
    Task<AuthResult<AuthResponse>> RegisterTenantAsync(RegisterTenantRequest request, CancellationToken ct = default);
    Task<AuthResult<AuthResponse>> RegisterTenantWithOAuthAsync(RegisterTenantWithOAuthRequest request, CancellationToken ct = default);
    Task<AuthResult<AuthResponse>> LoginTenantAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResult<AuthResponse>> LoginTenantWithOAuthAsync(LoginWithOAuthRequest request, CancellationToken ct = default);

    // Tenant User
    Task<AuthResult<AuthResponse>> LoginTenantUserAsync(LoginRequest request, CancellationToken ct = default);

    // Public User
    Task<AuthResult<AuthResponse>> RegisterPublicUserAsync(Guid tenantId, RegisterPublicUserRequest request, CancellationToken ct = default);
    Task<AuthResult<AuthResponse>> RegisterPublicUserWithOAuthAsync(Guid tenantId, RegisterPublicUserWithOAuthRequest request, CancellationToken ct = default);
    Task<AuthResult<AuthResponse>> LoginPublicUserAsync(Guid tenantId, LoginPublicUserRequest request, CancellationToken ct = default);
    Task<AuthResult<AuthResponse>> LoginPublicUserWithOAuthAsync(Guid tenantId, LoginPublicUserWithOAuthRequest request, CancellationToken ct = default);

    // Common
    Task<AuthResult<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<AuthResult<bool>> LogoutAsync(Guid userId, AccountType accountType, string refreshToken, CancellationToken ct = default);
}

public sealed class AuthService : IAuthService
{
    private readonly ITenantAccountRepository _tenantAccountRepo;
    private readonly ITenantUserRepository _tenantUserRepo;
    private readonly IPublicUserRepository _publicUserRepo;
    private readonly IRefreshTokenRepository _tokenRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IOAuthService _oauthService;
    private readonly IMapper _mapper;

    public AuthService(
        ITenantAccountRepository tenantAccountRepo,
        ITenantUserRepository tenantUserRepo,
        IPublicUserRepository publicUserRepo,
        IRefreshTokenRepository tokenRepo,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IOAuthService oauthService,
        IMapper mapper)
    {
        _tenantAccountRepo = tenantAccountRepo;
        _tenantUserRepo = tenantUserRepo;
        _publicUserRepo = publicUserRepo;
        _tokenRepo = tokenRepo;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _oauthService = oauthService;
        _mapper = mapper;
    }

    // ===== TENANT ACCOUNT =====

    public async Task<AuthResult<AuthResponse>> RegisterTenantAsync(RegisterTenantRequest request, CancellationToken ct = default)
    {
        var email = Email.Create(request.Email);

        if (await _tenantAccountRepo.ExistsAsync(email, ct))
            return AuthError.UserAlreadyExists;

        var passwordHash = PasswordHash.Create(_passwordHasher.Hash(request.Password));
        var tenantId = Guid.NewGuid();

        var account = TenantAccount.CreateWithPassword(tenantId, email, passwordHash, request.Name);
        await _tenantAccountRepo.AddAsync(account, ct);

        return await CreateAuthResponse(account, request.IpAddress, request.UserAgent, ct);
    }

    public async Task<AuthResult<AuthResponse>> RegisterTenantWithOAuthAsync(RegisterTenantWithOAuthRequest request, CancellationToken ct = default)
    {
        var oauthInfo = await _oauthService.ValidateGoogleTokenAsync(request.IdToken, ct);
        if (oauthInfo is null || !oauthInfo.EmailVerified)
            return AuthError.OAuthError;

        var email = Email.Create(oauthInfo.Email);
        var existing = await _tenantAccountRepo.GetByOAuthAsync(oauthInfo.OAuthId, OAuthProvider.Google, ct);

        if (existing is not null)
            return await CreateAuthResponse(existing, null, null, ct);

        if (await _tenantAccountRepo.ExistsAsync(email, ct))
            return AuthError.UserAlreadyExists;

        var tenantId = Guid.NewGuid();
        var account = TenantAccount.CreateWithOAuth(tenantId, email, oauthInfo.OAuthId, OAuthProvider.Google, oauthInfo.Name);

        await _tenantAccountRepo.AddAsync(account, ct);

        return await CreateAuthResponse(account, null, null, ct);
    }

    public async Task<AuthResult<AuthResponse>> LoginTenantAsync(LoginRequest request, CancellationToken ct = default)
    {
        var account = await _tenantAccountRepo.GetByEmailAsync(request.Email, ct);

        if (account is null)
            return AuthError.InvalidCredentials;

        if (!account.IsActive)
            return AuthError.UserInactive;

        if (account.PasswordHash is null || !_passwordHasher.Verify(request.Password, account.PasswordHash))
            return AuthError.InvalidCredentials;

        account.UpdateLastLogin();
        await _tenantAccountRepo.UpdateAsync(account, ct);

        return await CreateAuthResponse(account, request.IpAddress, request.UserAgent, ct);
    }

    public async Task<AuthResult<AuthResponse>> LoginTenantWithOAuthAsync(LoginWithOAuthRequest request, CancellationToken ct = default)
    {
        var oauthInfo = await _oauthService.ValidateGoogleTokenAsync(request.IdToken, ct);
        if (oauthInfo is null || !oauthInfo.EmailVerified)
            return AuthError.OAuthError;

        var account = await _tenantAccountRepo.GetByOAuthAsync(oauthInfo.OAuthId, OAuthProvider.Google, ct);

        if (account is null)
            return AuthError.UserNotFound;

        if (!account.IsActive)
            return AuthError.UserInactive;

        account.UpdateLastLogin();
        await _tenantAccountRepo.UpdateAsync(account, ct);

        return await CreateAuthResponse(account, request.IpAddress, request.UserAgent, ct);
    }

    private async Task<AuthResponse> CreateAuthResponse(TenantAccount account, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        var userInfo = _mapper.Map<UserInfo>(account);
        var claims = new TokenClaims(
            userInfo.Id,
            userInfo.AccountType,
            userInfo.TenantId,
            userInfo.Email,
            userInfo.Name,
            userInfo.ModuleAccesses);

        var accessToken = _tokenGenerator.GenerateAccessToken(claims);
        var refreshToken = _tokenGenerator.GenerateRefreshToken();

        var token = RefreshToken.Create(
            refreshToken,
            account.Id,
            AccountType.TenantOwner,
            account.TenantId,
            TimeSpan.FromDays(7),
            ipAddress,
            userAgent);

        await _tokenRepo.AddAsync(token, ct);

        return new AuthResponse(accessToken, refreshToken, token.ExpiresAt, userInfo);
    }

    public async Task<AuthResult<AuthResponse>> LoginTenantUserAsync(LoginRequest request, CancellationToken ct = default)
    {
        // Precisa buscar em todos os tenants já que não temos o tenantId no request
        // Alternativa: passar tenantId no request ou ter domínio único por tenant
        // Por ora, vou buscar só por email (assumindo que é único globalmente)

        // NOTA: Esta abordagem requer ajuste - em produção você deve ter subdomain ou tenantId no request
        return AuthError.Custom("TENANT_USER_LOGIN", "TenantUser login requires tenant identification");
    }

    // ===== PUBLIC USER =====

    public async Task<AuthResult<AuthResponse>> RegisterPublicUserAsync(Guid tenantId, RegisterPublicUserRequest request, CancellationToken ct = default)
    {
        var email = Email.Create(request.Email);

        if (await _publicUserRepo.ExistsAsync(tenantId, request.Module, email, ct))
            return AuthError.UserAlreadyExists;

        var passwordHash = PasswordHash.Create(_passwordHasher.Hash(request.Password));

        var user = PublicUser.CreateWithPassword(tenantId, request.Module, email, passwordHash, request.Name);
        await _publicUserRepo.AddAsync(user, ct);

        return await CreateAuthResponse(user, null, null, ct);
    }

    public async Task<AuthResult<AuthResponse>> RegisterPublicUserWithOAuthAsync(Guid tenantId, RegisterPublicUserWithOAuthRequest request, CancellationToken ct = default)
    {
        var oauthInfo = await _oauthService.ValidateGoogleTokenAsync(request.IdToken, ct);
        if (oauthInfo is null || !oauthInfo.EmailVerified)
            return AuthError.OAuthError;

        var existing = await _publicUserRepo.GetByOAuthAsync(tenantId, request.Module, oauthInfo.OAuthId, OAuthProvider.Google, ct);

        if (existing is not null)
        {
            if (existing.IsDeleted)
                return AuthError.UserDeleted;

            return await CreateAuthResponse(existing, null, null, ct);
        }

        var email = Email.Create(oauthInfo.Email);
        if (await _publicUserRepo.ExistsAsync(tenantId, request.Module, email, ct))
            return AuthError.UserAlreadyExists;

        var user = PublicUser.CreateWithOAuth(tenantId, request.Module, email, oauthInfo.OAuthId, OAuthProvider.Google, oauthInfo.Name);
        await _publicUserRepo.AddAsync(user, ct);

        return await CreateAuthResponse(user, null, null, ct);
    }

    public async Task<AuthResult<AuthResponse>> LoginPublicUserAsync(Guid tenantId, LoginPublicUserRequest request, CancellationToken ct = default)
    {
        var user = await _publicUserRepo.GetByEmailAsync(tenantId, request.Module, request.Email, ct);

        if (user is null)
            return AuthError.InvalidCredentials;

        if (user.IsDeleted)
            return AuthError.UserDeleted;

        if (!user.IsActive)
            return AuthError.UserInactive;

        if (user.PasswordHash is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return AuthError.InvalidCredentials;

        user.UpdateLastLogin();
        await _publicUserRepo.UpdateAsync(user, ct);

        return await CreateAuthResponse(user, request.IpAddress, request.UserAgent, ct);
    }

    public async Task<AuthResult<AuthResponse>> LoginPublicUserWithOAuthAsync(Guid tenantId, LoginPublicUserWithOAuthRequest request, CancellationToken ct = default)
    {
        var oauthInfo = await _oauthService.ValidateGoogleTokenAsync(request.IdToken, ct);
        if (oauthInfo is null || !oauthInfo.EmailVerified)
            return AuthError.OAuthError;

        var user = await _publicUserRepo.GetByOAuthAsync(tenantId, request.Module, oauthInfo.OAuthId, OAuthProvider.Google, ct);

        if (user is null)
            return AuthError.UserNotFound;

        if (user.IsDeleted)
            return AuthError.UserDeleted;

        if (!user.IsActive)
            return AuthError.UserInactive;

        user.UpdateLastLogin();
        await _publicUserRepo.UpdateAsync(user, ct);

        return await CreateAuthResponse(user, request.IpAddress, request.UserAgent, ct);
    }

    private async Task<AuthResponse> CreateAuthResponse(TenantUser user, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        var userInfo = _mapper.Map<UserInfo>(user);
        var claims = new TokenClaims(
            userInfo.Id,
            userInfo.AccountType,
            userInfo.TenantId,
            userInfo.Email,
            userInfo.Name,
            userInfo.ModuleAccesses);

        var accessToken = _tokenGenerator.GenerateAccessToken(claims);
        var refreshToken = _tokenGenerator.GenerateRefreshToken();

        var token = RefreshToken.Create(
            refreshToken,
            user.Id,
            AccountType.TenantUser,
            user.TenantId,
            TimeSpan.FromDays(7),
            ipAddress,
            userAgent);

        await _tokenRepo.AddAsync(token, ct);

        return new AuthResponse(accessToken, refreshToken, token.ExpiresAt, userInfo);
    }

    private async Task<AuthResponse> CreateAuthResponse(PublicUser user, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        var userInfo = _mapper.Map<UserInfo>(user);
        var claims = new TokenClaims(
            userInfo.Id,
            userInfo.AccountType,
            userInfo.TenantId,
            userInfo.Email,
            userInfo.Name,
            userInfo.ModuleAccesses);

        var accessToken = _tokenGenerator.GenerateAccessToken(claims);
        var refreshToken = _tokenGenerator.GenerateRefreshToken();

        var token = RefreshToken.Create(
            refreshToken,
            user.Id,
            AccountType.PublicUser,
            user.TenantId,
            TimeSpan.FromDays(7),
            ipAddress,
            userAgent);

        await _tokenRepo.AddAsync(token, ct);

        return new AuthResponse(accessToken, refreshToken, token.ExpiresAt, userInfo);
    }

    // ===== COMMON =====

    public async Task<AuthResult<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var token = await _tokenRepo.GetByTokenAsync(request.RefreshToken, ct);

        if (token is null || !token.IsValid)
            return AuthError.InvalidToken;

        // Busca o usuário baseado no tipo de conta
        UserInfo? userInfo = token.AccountType switch
        {
            AccountType.TenantOwner => await GetTenantAccountUserInfo(token.UserId, ct),
            AccountType.TenantUser => await GetTenantUserUserInfo(token.UserId, ct),
            AccountType.PublicUser => await GetPublicUserUserInfo(token.UserId, ct),
            _ => null
        };

        if (userInfo is null)
            return AuthError.UserNotFound;

        var claims = new TokenClaims(
            userInfo.Id,
            userInfo.AccountType,
            userInfo.TenantId,
            userInfo.Email,
            userInfo.Name,
            userInfo.ModuleAccesses);

        var accessToken = _tokenGenerator.GenerateAccessToken(claims);

        return new AuthResponse(accessToken, request.RefreshToken, token.ExpiresAt, userInfo);
    }

    public async Task<AuthResult<bool>> LogoutAsync(Guid userId, AccountType accountType, string refreshToken, CancellationToken ct = default)
    {
        var token = await _tokenRepo.GetByTokenAsync(refreshToken, ct);

        if (token is null || token.UserId != userId || token.AccountType != accountType)
            return AuthError.InvalidToken;

        token.Revoke("User logout");
        await _tokenRepo.UpdateAsync(token, ct);

        return true;
    }

    private async Task<UserInfo?> GetTenantAccountUserInfo(Guid userId, CancellationToken ct)
    {
        var account = await _tenantAccountRepo.GetByIdAsync(userId, ct);
        return account is not null && account.IsActive ? _mapper.Map<UserInfo>(account) : null;
    }

    private async Task<UserInfo?> GetTenantUserUserInfo(Guid userId, CancellationToken ct)
    {
        var user = await _tenantUserRepo.GetByIdAsync(userId, ct);
        return user is not null && user.IsActive ? _mapper.Map<UserInfo>(user) : null;
    }

    private async Task<UserInfo?> GetPublicUserUserInfo(Guid userId, CancellationToken ct)
    {
        var user = await _publicUserRepo.GetByIdAsync(userId, ct);
        return user is not null && user.IsActive && !user.IsDeleted ? _mapper.Map<UserInfo>(user) : null;
    }
}
