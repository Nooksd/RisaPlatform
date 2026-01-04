namespace Auth.Api.DTOs;

public sealed record UpdateTenantUserRequest(
    string Name,
    Dictionary<string, int> ModuleAccesses);