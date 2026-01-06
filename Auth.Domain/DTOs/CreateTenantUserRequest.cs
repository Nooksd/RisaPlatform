namespace Auth.Domain.DTOs;

public sealed record CreateTenantUserRequest(
    Guid TenantId,
    string Email,
    string Username,
    string Password,
    string Name,
    Dictionary<string, int> ModuleAccesses);