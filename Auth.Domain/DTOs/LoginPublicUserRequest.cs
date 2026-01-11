namespace Auth.Domain.DTOs;

public sealed record LoginPublicUserRequest(
    string Module,
    Guid TenantId,
    string Email,
    string Password);