using Auth.Domain.DTOs;
using Auth.Domain.Enums;
using Auth.Domain.Results;

namespace Auth.Domain.Interfaces.Services;

public interface IAuthService
{
    // Tenant Account
    Task<AuthResult<AuthResponse>> RegisterTenantAsync(RegisterTenantRequest request, string ipAddress, string userAgent, CancellationToken ct = default);
    Task<AuthResult<AuthResponse>> RegisterTenantWithOAuthAsync(RegisterTenantWithOAuthRequest request, CancellationToken ct = default);
    Task<AuthResult<AuthResponse>> LoginTenantAsync(LoginRequest request, string ipAddress, string userAgent, CancellationToken ct = default);
    Task<AuthResult<AuthResponse>> LoginTenantWithOAuthAsync(LoginWithOAuthRequest request, string ipAddress, string userAgent, CancellationToken ct = default);

    // Tenant User
    Task<AuthResult<AuthResponse>> LoginTenantUserAsync(TenantUserLoginRequest request, string ipAddress, string userAgent, CancellationToken ct = default);

    // Public User
    Task<AuthResult<AuthResponse>> RegisterPublicUserAsync(Guid tenantId, RegisterPublicUserRequest request, CancellationToken ct = default);
    Task<AuthResult<AuthResponse>> RegisterPublicUserWithOAuthAsync(Guid tenantId, RegisterPublicUserWithOAuthRequest request, CancellationToken ct = default);
    Task<AuthResult<AuthResponse>> LoginPublicUserAsync(Guid tenantId, LoginPublicUserRequest request, string ipAddress, string userAgent, CancellationToken ct = default);
    Task<AuthResult<AuthResponse>> LoginPublicUserWithOAuthAsync(Guid tenantId, LoginPublicUserWithOAuthRequest request, string ipAddress, string userAgent, CancellationToken ct = default);

    // Common
    Task<AuthResult<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<AuthResult<bool>> LogoutAsync(Guid userId, AccountType accountType, string refreshToken, CancellationToken ct = default);
}
