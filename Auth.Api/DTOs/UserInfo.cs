namespace Auth.Api.DTOs;

public sealed record UserInfo(
    Guid Id,
    string Email,
    string Name,
    string AccountType,
    Guid TenantId,
    Dictionary<string, int> ModuleAccesses);