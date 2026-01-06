namespace Auth.Domain.DTOs;

public sealed record UpdateTenantUserRequest(
    string Name,
    Dictionary<string, int> ModuleAccesses);