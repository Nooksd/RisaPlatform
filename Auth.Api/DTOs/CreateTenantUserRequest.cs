namespace Auth.Api.DTOs;

public sealed record CreateTenantUserRequest(
    string Email,
    string Password,
    string Name,
    Dictionary<string, int> ModuleAccesses);