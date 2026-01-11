namespace Auth.Domain.DTOs;

public sealed record RegisterPublicUserRequest(
    string Module,
    Guid TenantId,
    string Email,
    string Password,
    string Name);

