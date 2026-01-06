namespace Auth.Domain.DTOs;

public sealed record TenantUserLoginRequest(
    Guid TenantId,
    string? Email,
    string? Username,
    string Password);
