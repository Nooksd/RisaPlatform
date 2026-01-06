namespace Auth.Domain.DTOs;

public sealed record UserInfo(
    Guid Id,
    string Email,
    string? Name,
    string? UserName,
    string AccountType,
    List<Guid> TenantIds,
    Dictionary<string, int> ModuleAccesses);